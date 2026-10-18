"""Read-only Grim Dawn ARZ v3 / ARC v3 reader. Python standard library only.

No save formats, writing/repacking, DLL loading or game execution are implemented.
"""
from __future__ import annotations
import argparse, fnmatch, hashlib, json, struct, zlib, re, mmap
from pathlib import Path

GAME_ROOT = Path(r'D:\Steam\steamapps\common\Grim Dawn')
MAX_OBJECT = 64 * 1024 * 1024
class FormatError(ValueError): pass

def span(data: bytes, start: int, size: int) -> bytes:
    if start < 0 or size < 0 or start > len(data) or size > len(data)-start:
        raise FormatError(f'Out-of-range extent {start}+{size}, file size {len(data)}')
    return data[start:start+size]

def lz4_block(data: bytes, expected: int) -> bytes:
    """Decode a raw LZ4 block with a caller-supplied exact output bound."""
    if not 0 <= expected <= MAX_OBJECT: raise FormatError('LZ4 output limit exceeded')
    out=bytearray(); p=0
    def length(n):
        nonlocal p
        if n==15:
            while True:
                if p>=len(data): raise FormatError('Truncated LZ4 length')
                x=data[p];p+=1;n+=x
                if x!=255:break
                if n>expected:raise FormatError('LZ4 length exceeds output bound')
        return n
    while p<len(data):
        token=data[p];p+=1;n=length(token>>4)
        if n>expected-len(out):raise FormatError('LZ4 literal output overflow')
        out.extend(span(data,p,n));p+=n
        if p==len(data):break
        distance=struct.unpack('<H',span(data,p,2))[0];p+=2
        if distance==0 or distance>len(out):raise FormatError('Invalid LZ4 match distance')
        n=length(token&15)+4
        if n>expected-len(out):raise FormatError('LZ4 match output overflow')
        while n:
            take=min(n,distance);out.extend(out[-distance:][:take]);n-=take
    if len(out)!=expected:raise FormatError(f'LZ4 size mismatch {len(out)} != {expected}')
    return bytes(out)

def input_path(path: str|Path) -> Path:
    p=Path(path).resolve(strict=True);root=GAME_ROOT.resolve(strict=True)
    if not p.is_relative_to(root) or p.suffix.lower() not in {'.arc','.arz'}:
        raise ValueError('Input must be an .arc/.arz under the explicitly authorized game installation')
    if not p.is_file():raise ValueError('Input is not a regular file')
    return p

def key(name: str) -> str:
    return name.replace('\\','/').rstrip('\0').casefold()

class ReadOnlyContainer:
    @classmethod
    def open(cls,path):
        p=input_path(path);f=p.open('rb');mapping=None
        try:
            mapping=mmap.mmap(f.fileno(),0,access=mmap.ACCESS_READ);obj=cls(mapping)
            obj.path=str(p);obj._file=f;obj._mapping=mapping;return obj
        except BaseException:
            if mapping is not None:mapping.close()
            f.close();raise
    def close(self):
        m=getattr(self,'_mapping',None)
        if m is not None:m.close();self._mapping=None
        f=getattr(self,'_file',None)
        if f is not None:f.close();self._file=None
    def __enter__(self):return self
    def __exit__(self,*args):self.close()
    def __del__(self):self.close()

class ARZ(ReadOnlyContainer):
    def __init__(self, data: bytes):
        self.data=data
        self.marker,self.version,rt,rs,count,st,ss=struct.unpack('<HHIIIII',span(data,0,24))
        if (self.marker,self.version)!=(2,3):raise FormatError('Unsupported ARZ marker/version')
        if count>1_000_000:raise FormatError('ARZ record count limit')
        span(data,rt,rs); strings_data=span(data,st,ss)
        self.header={'marker':self.marker,'version':self.version,'record_table_offset':rt,'record_table_size':rs,'records':count,'string_table_offset':st,'string_table_size':ss}
        self.strings=[];p=0
        def u32():
            nonlocal p
            v=struct.unpack('<I',span(strings_data,p,4))[0];p+=4;return v
        n=u32()
        if n>2_000_000:raise FormatError('ARZ string count limit')
        for _ in range(n):
            size=u32();self.strings.append(span(strings_data,p,size).decode('utf-8'));p+=size
        if p!=ss:raise FormatError('ARZ string table length mismatch')
        self.records={};p=rt
        for _ in range(count):
            idx,ln=struct.unpack('<II',span(data,p,8));p+=8
            kind=span(data,p,ln).decode('ascii');p+=ln
            offset,packed,unpacked,time1,time2=struct.unpack('<IIIII',span(data,p,20));p+=20
            name=self.string(idx)
            if key(name) in self.records:raise FormatError('Duplicate ARZ record')
            if unpacked>MAX_OBJECT:raise FormatError('ARZ record size limit')
            span(data,offset+24,packed)
            if offset+24+packed>rt:raise FormatError('ARZ record overlaps tables')
            self.records[key(name)]={'name':name,'class':kind,'offset':offset+24,'packed':packed,'unpacked':unpacked,'timestamp_words':[time1,time2]}
        if p!=rt+rs:raise FormatError('ARZ record table length mismatch')

    def string(self,i):
        if not 0<=i<len(self.strings):raise FormatError(f'ARZ string index {i} outside table')
        return self.strings[i]
    def record(self,name):
        entry=self.records[key(name)];raw=span(self.data,entry['offset'],entry['packed'])
        body=lz4_block(raw,entry['unpacked']);p=0;fields={};types={}
        while p<len(body):
            typ,count,idx=struct.unpack('<HHI',span(body,p,8));p+=8;field=self.string(idx)
            if typ not in (0,1,2,3):raise FormatError(f'Unknown ARZ field type {typ}')
            if field in fields:raise FormatError('Duplicate ARZ field')
            buf=span(body,p,count*4);p+=count*4
            if typ==1:vals=list(struct.unpack('<'+'f'*count,buf))
            elif typ==2:vals=[self.string(x) for x in struct.unpack('<'+'I'*count,buf)]
            else:vals=list(struct.unpack('<'+'i'*count,buf))
            fields[field]=vals[0] if count==1 else vals;types[field]=typ
        return {'name':entry['name'],'class':entry['class'],'fields':fields,'field_types':types,'body_sha256':hashlib.sha256(body).hexdigest()}

class ARC(ReadOnlyContainer):
    def __init__(self,data: bytes):
        self.data=data
        magic,ver,count,parts,ps,ns,po=struct.unpack('<7I',span(data,0,28))
        if (magic,ver)!=(0x435241,3):raise FormatError('Unsupported ARC marker/version')
        if count>1_000_000 or parts>4_000_000:raise FormatError('ARC count limit')
        if ps!=parts*12:raise FormatError('ARC part table size mismatch')
        span(data,po,ps);names=span(data,po+ps,ns);rh=po+ps+ns;span(data,rh,count*44)
        self.header={'magic':hex(magic),'version':ver,'entries':count,'parts':parts,'part_table_offset':po,'part_table_size':ps,'name_table_size':ns,'entry_table_offset':rh}
        self.parts=[struct.unpack('<III',span(data,po+i*12,12)) for i in range(parts)];self.entries={}
        for i in range(count):
            typ,offset,packed,unpacked,checksum,filetime,npart,first,nlen,noffset=struct.unpack('<IIIIIQIIII',span(data,rh+i*44,44))
            name=span(names,noffset,nlen).rstrip(b'\0').decode('ascii')
            if key(name) in self.entries:raise FormatError('Duplicate ARC name')
            if first+npart>parts or unpacked>MAX_OBJECT:raise FormatError('ARC part/size bounds')
            self.entries[key(name)]={'name':name,'type':typ,'offset':offset,'packed':packed,'unpacked':unpacked,'checksum':checksum,'filetime':filetime,'parts':npart,'first':first}
    def read(self,name):
        e=self.entries[key(name)]
        if e['type']==1 and e['packed']==e['unpacked']:
            body=span(self.data,e['offset'],e['packed'])
        else:
            chunks=[];packed_total=0;unpacked_total=0
            for offset,packed,unpacked in self.parts[e['first']:e['first']+e['parts']]:
                raw=span(self.data,offset,packed);chunks.append(raw if packed==unpacked else lz4_block(raw,unpacked));packed_total+=packed;unpacked_total+=unpacked
                if unpacked_total>e['unpacked']:raise FormatError('ARC aggregate output overflow')
            if packed_total!=e['packed']:raise FormatError('ARC packed sum mismatch')
            body=b''.join(chunks)
        if len(body)!=e['unpacked']:raise FormatError('ARC size mismatch')
        if zlib.adler32(body)&0xffffffff!=e['checksum']:raise FormatError('ARC Adler32 mismatch')
        return body
    def tags(self):
        result={};locations={}
        for e in self.entries.values():
            if not e['name'].lower().endswith('.txt'):continue
            body=self.read(e['name'])
            text=body.decode('utf-16') if body.startswith((b'\xff\xfe',b'\xfe\xff')) else body.decode('utf-8-sig')
            for line_no,line in enumerate(text.splitlines(),1):
                if '=' not in line or line.lstrip().startswith('//'):continue
                k,v=line.split('=',1);k=k.strip()
                if k:result[k]=v.strip();locations[k]={'archive_member':e['name'],'line':line_no}
        return result,locations

def references(record):
    result=[]
    for field,values in record['fields'].items():
        for v in values if isinstance(values,list) else [values]:
            if isinstance(v,str) and (v.lower().endswith(('.dbr','.qst','.cnv')) or v.lower().startswith('tag')):
                result.append({'field':field,'target':v,'kind':'tag' if v.lower().startswith('tag') else 'resource'})
    return result

def quest_text_view(data: bytes):
    """QST2 v9 envelope plus bounded validated length-prefixed text observations.

    Does not interpret condition/action opcodes or reconstruct the quest graph.
    Every string keeps its prefix offset for independent inspection.
    """
    if span(data,0,4)!=b'QST2' or struct.unpack('<I',span(data,4,4))[0]!=9:
        raise FormatError('Unsupported QST2 version')
    if len(data)>8*1024*1024:raise FormatError('QST text-view size limit')
    strings=[];i=8
    while i+4<=len(data):
        n=struct.unpack_from('<I',data,i)[0]
        if 3<=n<=8192 and i+4+n<=len(data):
            raw=data[i+4:i+4+n]
            try:s=raw.decode('utf-8')
            except UnicodeDecodeError:s=''
            if s and all(c.isprintable() or c in '\r\n\t' for c in s) and any(c.isalpha() for c in s):
                strings.append({'length_prefix_offset':i,'byte_length':n,'text':s});i+=4+n;continue
        i+=1
    refs=[x for x in strings if re.search(r'(?i)\.(dbr|qst|cnv)(?:@|$)',x['text'])]
    return {'magic':'QST2','version':9,'bytes':len(data),'sha256':hashlib.sha256(data).hexdigest(),'length_prefixed_strings':strings,'resource_references':refs,'coverage':'envelope and validated string observations only; scanning is not a complete quest graph/action parser'}

def quest_record(data: bytes):
    """Parse QST2 v9 localization directory exactly; preserve graph references.

    Compiled condition/action bytecode preceding the locale directory is opaque.
    Reference observations there are length-validated candidates, not graph edges.
    """
    view=quest_text_view(data);start=struct.unpack('<I',span(data,8,4))[0]
    if not 12<=start<=len(data)-4:raise FormatError('QST2 locale offset outside payload')
    p=start
    def u32():
        nonlocal p
        v=struct.unpack('<I',span(data,p,4))[0];p+=4;return v
    def string():
        nonlocal p
        offset=p;n=u32()
        if n>MAX_OBJECT:raise FormatError('QST2 string size limit')
        s=span(data,p,n).decode('utf-8');p+=n
        return {'length_prefix_offset':offset,'byte_length':n,'text':s}
    count=u32()
    if not 1<=count<=128:raise FormatError('QST2 locale count limit')
    locales={}
    for _ in range(count):
        lang=string()['text'];n=u32()
        if lang in locales or n>100000:raise FormatError('QST2 invalid locale/string count')
        locales[lang]=[string() for _ in range(n)]
    if p!=len(data):raise FormatError('QST2 locale section not fully consumed')
    return {'magic':'QST2','version':9,'bytes':len(data),'sha256':view['sha256'],'text_section_offset':start,'locales':locales,'resource_reference_candidates':[r for r in view['resource_references'] if r['length_prefix_offset']<start],'coverage':{'localization_directory':'fully consumed with bounds checks','condition_action_graph':'opaque; not reconstructed','resource_references':'length-prefixed string candidates, not interpreted actions'}}

def main():
    ap=argparse.ArgumentParser(description=__doc__);ap.add_argument('path');ap.add_argument('--match',default='*');ap.add_argument('--record');ap.add_argument('--quest-member');ap.add_argument('--tags',action='store_true');ap.add_argument('--limit',type=int,default=50);a=ap.parse_args()
    obj=ARZ.open(a.path) if Path(a.path).suffix.lower()=='.arz' else ARC.open(a.path)
    if a.quest_member:result=quest_record(obj.read(a.quest_member))
    elif a.record:result=obj.record(a.record) if isinstance(obj,ARZ) else {'name':a.record,'size':len(obj.read(a.record)),'sha256':hashlib.sha256(obj.read(a.record)).hexdigest()}
    elif a.tags:result=obj.tags()[0]
    else:
        matches=[v for k,v in (obj.records if isinstance(obj,ARZ) else obj.entries).items() if fnmatch.fnmatch(k,a.match.casefold())]
        if a.limit<1:ap.error('--limit must be positive')
        result={'header':obj.header,'matched_count':len(matches),'records':matches[:a.limit],'shown':min(len(matches),a.limit)}
    print(json.dumps(result,ensure_ascii=False,indent=2))
if __name__=='__main__':main()
