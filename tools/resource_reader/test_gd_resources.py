import struct as s
import unittest
import zlib
from gd_resources import ARZ, ARC, FormatError, lz4_block, quest_record, input_path

def literal(b):
    n=len(b)
    if n<15:return bytes([n<<4])+b
    rest=n-15; ext=bytearray()
    while rest>=255:ext.append(255);rest-=255
    return b'\xf0'+ext+bytes([rest])+b

def arz(typ=0, idx=1):
    strings=[b'records/test.dbr',b'zero',b'name',b'Test',b'arr',b'float']
    body=s.pack('<HHIi',typ,1,idx,0)+s.pack('<HHII',2,1,2,3)+s.pack('<HHIii',3,2,4,0,-2)+s.pack('<HHIf',1,1,5,1.25)
    packed=literal(body); kind=b'TestClass'
    table=s.pack('<II',0,len(kind))+kind+s.pack('<IIIII',0,len(packed),len(body),0,0)
    st=s.pack('<I',len(strings))+b''.join(s.pack('<I',len(x))+x for x in strings)
    return s.pack('<HHIIIII',2,3,24+len(packed),len(table),1,24+len(packed)+len(table),len(st))+packed+table+st

def arc(multipart=False, corrupt=False):
    body=b'hello hello'; pieces=[b'hello ',b'hello'] if multipart else [body]
    payload=b''; parts=[]
    for i,b in enumerate(pieces):
        raw=literal(b) if multipart and i==1 else b
        parts.append((28+len(payload),len(raw),len(b)));payload+=raw
    names=b'test.txt\0'; pt=b''.join(s.pack('<III',*x) for x in parts)
    e=s.pack('<IIIIIQIIII',3 if multipart else 1,28,len(payload),len(body),(zlib.adler32(body)+(1 if corrupt else 0))&0xffffffff,0,len(parts),0,len(names),0)
    return s.pack('<7I',0x435241,3,1,len(parts),len(pt),len(names),28+len(payload))+payload+pt+names+e

def quest():
    def st(b):return s.pack('<I',len(b))+b
    return b'QST2'+s.pack('<II',9,12)+s.pack('<I',1)+st(b'enUS')+s.pack('<I',2)+st(b'Test quest')+st(b'')

class ReaderTests(unittest.TestCase):
    def test_lz4_literals_and_overlap(self):
        self.assertEqual(lz4_block(literal(b'x'*300),300),b'x'*300)
        self.assertEqual(lz4_block(b'\x14a\x01\x00',9),b'a'*9)
    def test_lz4_rejects_corruption(self):
        for raw,n in [(b'\xf0',20),(b'\x10a\0\0',5),(b'\x10a\2\0',5),(b'\x14a\1\0',8),(b'\x20a',2)]:
            with self.subTest(raw=raw),self.assertRaises(FormatError):lz4_block(raw,n)
    def test_arz_types_zero_array(self):
        r=ARZ(arz()).record('RECORDS\\test.dbr')['fields']
        self.assertEqual(r,{'zero':0,'name':'Test','arr':[0,-2],'float':1.25})
    def test_arz_bad_type_and_index(self):
        for data in [arz(9),arz(idx=999)]:
            with self.assertRaises(FormatError):ARZ(data).record('records/test.dbr')
    def test_arc_raw_and_mixed_parts(self):
        for multi in [False,True]:self.assertEqual(ARC(arc(multi)).read('test.txt'),b'hello hello')
    def test_arc_checksum_and_truncation(self):
        with self.assertRaises(FormatError):ARC(arc(corrupt=True)).read('test.txt')
        with self.assertRaises(FormatError):ARC(arc()[:-1])
    def test_quest_exact_locale_including_empty(self):
        q=quest_record(quest())
        self.assertEqual([x['text'] for x in q['locales']['enUS']],['Test quest',''])
    def test_quest_rejects_trailing_and_truncated(self):
        for data in [quest()[:-1],quest()+b'x']:
            with self.assertRaises(FormatError):quest_record(data)
    def test_arz_truncated_tables(self):
        with self.assertRaises(FormatError):ARZ(arz()[:-1])
    def test_whitelist_rejects_existing_non_resource(self):
        with self.assertRaises(ValueError):input_path(__file__)

if __name__=='__main__':unittest.main(verbosity=2)
