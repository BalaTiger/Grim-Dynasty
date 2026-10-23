using System;
using System.Text;
using GrimDynasty.TaskItems;
class RecordTableGetterTests {
 static int n;
 static void Need(bool value,string message){n++;if(!value)throw new Exception(message);}
 static byte[] B(string value){return Encoding.ASCII.GetBytes(value);}
 static byte[] C(string value){byte[] bytes=B(value);Array.Resize(ref bytes,bytes.Length+1);return bytes;}
 static byte[] Repeated(int count,byte value){var bytes=new byte[count];for(int i=0;i<count;i++)bytes[i]=value;return bytes;}
 static byte[] Terminated(byte[] payload){var bytes=new byte[payload.Length+1];Array.Copy(payload,bytes,payload.Length);return bytes;}
 static TableString S(string value){return new TableString(B(value));}
 static LoadTableField F(string key,string value){return new LoadTableField(S(key),S(value));}
 static LoadTableSnapshot Table(params LoadTableField[] fields){return new LoadTableSnapshot(new RegistrationGate(),fields);}
 static bool Same(byte[] left,byte[] right){if(left.Length!=right.Length)return false;for(int i=0;i<left.Length;i++)if(left[i]!=right[i])return false;return true;}
 static bool Released(RecordTableGetterResult result,RecordTableSnapshot table){return result.GateAcquired&&result.GateReleased&&!table.Gate.IsHeldByCurrentThread;}
 static bool ArgumentRejected(Action action){try{action();return false;}catch(ArgumentException){return true;}}
 static void IdentityAndCString(){
  var empty=F("","empty-key");var item=F("Class","Item");var emptyValue=F("Empty","");
  var nulValue=new TableString(new byte[]{0,0x78});var withNul=new LoadTableField(S("NulValue"),nulValue);
  var table=Table(empty,item,emptyValue,withNul);var fallback=S("fallback");
  var result=RecordTableGetter.GetValue(table,C("Class"),fallback);
  Need(result.Status=="Known"&&Object.ReferenceEquals(result.Value,item.Value)&&Object.ReferenceEquals(result.MatchedField,item)&&!result.UsedDefault&&result.KeyLength==5&&result.LocalKeyCapacity==15&&result.LastCompletedStage=="GateReleased"&&result.FailureStage==null&&Released(result,table),"a Class hit returns the exact stored value and field after releasing the gate");
  result=RecordTableGetter.GetValue(table,new byte[]{67,108,97,115,115,0,0xff,88},fallback);
  Need(result.Status=="Known"&&Object.ReferenceEquals(result.Value,item.Value)&&result.KeyLength==5&&Released(result,table),"query bytes after the first NUL are not part of the search key");
  result=RecordTableGetter.GetValue(table,C("class"),fallback);
  Need(result.Status=="Known"&&result.UsedDefault&&result.MatchedField==null&&Object.ReferenceEquals(result.Value,fallback),"field matching is case sensitive");
  result=RecordTableGetter.GetValue(table,C("\\Class"),fallback);
  Need(result.Status=="Known"&&result.UsedDefault&&Object.ReferenceEquals(result.Value,fallback),"the getter does not normalize paths or remove a leading separator");
  result=RecordTableGetter.GetValue(table,new byte[]{0,0xff},fallback);
  Need(result.Status=="Known"&&result.KeyLength==0&&result.LocalKeyCapacity==15&&Object.ReferenceEquals(result.Value,empty.Value)&&Object.ReferenceEquals(result.MatchedField,empty),"a terminated empty query can match a complete empty stored key");
  var embeddedKey=new LoadTableField(new TableString(new byte[]{67,108,97,115,115,0,88}),S("longer-key"));
  result=RecordTableGetter.GetValue(Table(embeddedKey),C("Class"),fallback);
  Need(result.Status=="Known"&&result.UsedDefault&&result.MatchedField==null&&Object.ReferenceEquals(result.Value,fallback),"a stored key keeps its full payload after NUL and does not match its shorter C-string prefix");
  result=RecordTableGetter.GetValue(table,C("Empty"),fallback);
  Need(result.Status=="Known"&&result.UsedDefault&&Object.ReferenceEquals(result.MatchedField,emptyValue)&&Object.ReferenceEquals(result.Value,fallback),"a matched zero-length stored value returns the original default identity");
  result=RecordTableGetter.GetValue(table,C("NulValue"),fallback);
  Need(result.Status=="Known"&&!result.UsedDefault&&Object.ReferenceEquals(result.Value,nulValue)&&result.Value.Length==2&&Same(result.Value.CopyCStringBytes(),new byte[]{0,0x78,0}),"a nonempty stored value beginning with NUL still returns its full value identity");
  result=RecordTableGetter.GetValue(table,C("missing"),null);
  Need(result.Status=="Known"&&result.UsedDefault&&result.Value==null&&result.MatchedField==null&&Released(result,table),"a missing key with a null default is a known null result");
  result=RecordTableGetter.GetValue(table,C("Empty"),null);
  Need(result.Status=="Known"&&result.UsedDefault&&result.Value==null&&Object.ReferenceEquals(result.MatchedField,emptyValue)&&Released(result,table),"an empty matched value preserves a known null default without erasing the match");
  var noFields=Table();result=RecordTableGetter.GetValue(noFields,C("Class"),fallback);
  Need(result.Status=="Known"&&result.UsedDefault&&result.MatchedField==null&&Object.ReferenceEquals(result.Value,fallback)&&result.KeyLength==5&&Released(result,noFields),"a complete empty tree returns the same default after reading a valid query key");
 }
 static void LowerBoundAndUnsigned(){
  var table=Table(F("B","first"),F("Class","middle"),F("ClassZ","prefix-successor"),F("Z","last"));var fallback=S("default");
  foreach(string query in new[]{"A","C","ClassA","ZZ"}){
   var result=RecordTableGetter.GetValue(table,C(query),fallback);
   Need(result.Status=="Known"&&result.UsedDefault&&result.MatchedField==null&&Object.ReferenceEquals(result.Value,fallback)&&Released(result,table),"lower_bound does not return an unequal candidate before, between or after keys: "+query);
  }
  byte[] keys={0x7f,0x80,0xff};var fields=new LoadTableField[keys.Length];
  for(int i=0;i<keys.Length;i++)fields[i]=new LoadTableField(new TableString(new[]{keys[i]}),new TableString(new[]{keys[i],(byte)0}));
  table=Table(fields);
  for(int i=0;i<keys.Length;i++){
   var result=RecordTableGetter.GetValue(table,new[]{keys[i],(byte)0},fallback);
   Need(result.Status=="Known"&&!result.UsedDefault&&Object.ReferenceEquals(result.MatchedField,fields[i])&&Object.ReferenceEquals(result.Value,fields[i].Value)&&Released(result,table),"ordered lookup uses unsigned bytes without locale or text decoding: "+keys[i]);
  }
 }
 static void SnapshotIsolation(){
  byte[] key=B("Class"),value=B("Item"),defaultBytes=B("fallback");var field=new LoadTableField(new TableString(key),new TableString(value));
  var list=new[]{field};var table=Table(list);var fallback=new TableString(defaultBytes);
  key[0]=0;value[0]=0;defaultBytes[0]=0;list[0]=F("Other","other");
  var result=RecordTableGetter.GetValue(table,C("Class"),fallback);
  Need(result.Status=="Known"&&table.Count==1&&Object.ReferenceEquals(result.MatchedField,field)&&Object.ReferenceEquals(result.Value,field.Value)&&Same(result.Value.CopyCStringBytes(),C("Item"))&&Same(fallback.CopyCStringBytes(),C("fallback")),"string and field-list snapshots copy their mutable inputs while retaining supplied storage objects");
  byte[] keyCopy=field.Key.CopyCStringBytes(),valueCopy=field.Value.CopyCStringBytes(),defaultCopy=fallback.CopyCStringBytes();keyCopy[0]=0;valueCopy[0]=0;defaultCopy[0]=0;
  result=RecordTableGetter.GetValue(table,C("Class"),fallback);
  Need(result.Status=="Known"&&Object.ReferenceEquals(result.Value,field.Value)&&Same(field.Key.CopyCStringBytes(),C("Class"))&&Same(field.Value.CopyCStringBytes(),C("Item"))&&Same(fallback.CopyCStringBytes(),C("fallback")),"exported C-string copies do not expose key, value or default storage");
  byte[] query=new byte[]{67,108,97,115,115,0,255};byte[] original=(byte[])query.Clone();
  result=RecordTableGetter.GetValue(table,query,fallback);
  Need(result.Status=="Known"&&Same(query,original)&&Object.ReferenceEquals(result.Value,field.Value)&&Released(result,table),"the getter leaves the query buffer, including its suffix, unchanged");
 }
 static void GuardsAndBinary(){
  var fallback=S("default");
  var unknown=RecordTableGetter.GetValue(new object(),C("Class"),fallback);var absent=RecordTableGetter.GetValue(null,C("Class"),fallback);
  Need(unknown.Status=="Unsupported"&&absent.Status=="Unsupported"&&unknown.Reason=="UnknownRecordTableSnapshotKind"&&absent.Reason==unknown.Reason&&!unknown.GateAcquired&&!absent.GateAcquired,"opaque and missing tables are not treated as empty ordinary tables");
  var missingGate=new LoadTableSnapshot(null,new LoadTableField[0]);var result=RecordTableGetter.GetValue(missingGate,C("Class"),fallback);
  Need(result.Status=="Unsupported"&&result.Reason=="UnknownRecordTableGate"&&result.FailureStage=="GateAcquire"&&!result.GateAcquired&&!result.GateReleased&&!result.KeyLength.HasValue,"unknown synchronization is rejected before reading a key");
  var table=Table();byte[][] invalid={null,new byte[0],new byte[]{0x41}};
  for(int i=0;i<invalid.Length;i++){
   result=RecordTableGetter.GetValue(table,invalid[i],fallback);
   Need(result.Status=="Unsupported"&&result.Reason==(i==0?"UnknownGetterKeyCString":"GetterKeyCStringUnterminatedOrOutsideCoveredDomain")&&result.FailureStage=="KeyCStringRead"&&result.LastCompletedStage=="GateAcquired"&&!result.KeyLength.HasValue&&result.Value==null&&!result.UsedDefault&&Released(result,table),"an ordinary empty table still requires a valid terminated key after acquiring the gate: "+i);
  }
  var binary=new LoadTableBinarySnapshot(new RegistrationGate(),false);
  result=RecordTableGetter.GetValue(binary,null,null);
  Need(result.Status=="Known"&&result.Value==null&&result.UsedDefault&&result.MatchedField==null&&!result.KeyLength.HasValue&&!result.LocalKeyCapacity.HasValue&&Released(result,binary),"an explicitly absent binary archive returns a known null default without reading a null key");
  result=RecordTableGetter.GetValue(binary,Repeated(65537,0x41),fallback);
  Need(result.Status=="Known"&&result.UsedDefault&&Object.ReferenceEquals(result.Value,fallback)&&!result.KeyLength.HasValue&&!result.LocalKeyCapacity.HasValue&&Released(result,binary),"the binary absent-archive branch ignores even an oversized unterminated key");
  foreach(bool? archive in new bool?[]{null,true}){
   binary=new LoadTableBinarySnapshot(new RegistrationGate(),archive);result=RecordTableGetter.GetValue(binary,null,fallback);
   Need(result.Status=="Unsupported"&&result.Reason==(archive.HasValue?"UnimplementedBinaryNameIdCache":"UnknownBinaryArchivePresence")&&result.FailureStage==(archive.HasValue?"BinaryNameToId":"BinaryArchiveRead")&&result.LastCompletedStage=="GateAcquired"&&result.Value==null&&!result.UsedDefault&&!result.KeyLength.HasValue&&Released(result,binary),"unknown or present binary archive does not become a fabricated default or pointer-cache hit");
  }
 }
 static void LengthBoundaries(){
  int[] lengths={15,16,65536};ulong[] capacities={15,31,65551};var fallback=S("default");
  for(int i=0;i<lengths.Length;i++){
   byte[] key=Repeated(lengths[i],0x41);var field=new LoadTableField(new TableString(key),S("value"));var table=Table(field);
   var result=RecordTableGetter.GetValue(table,Terminated(key),fallback);
   Need(result.Status=="Known"&&result.KeyLength==lengths[i]&&result.LocalKeyCapacity==capacities[i]&&Object.ReferenceEquals(result.Value,field.Value)&&Released(result,table),"a terminator immediately after the covered payload is accepted at length "+lengths[i]);
  }
  var empty=Table();var excessive=RecordTableGetter.GetValue(empty,Terminated(Repeated(65537,0x41)),fallback);
  Need(excessive.Status=="Unsupported"&&excessive.Reason=="GetterKeyCStringUnterminatedOrOutsideCoveredDomain"&&!excessive.KeyLength.HasValue&&excessive.Value==null&&Released(excessive,empty),"a first terminator after 65537 payload bytes remains outside the coverage budget");
  var noTerminator=RecordTableGetter.GetValue(empty,Repeated(65536,0x41),fallback);
  Need(noTerminator.Status=="Unsupported"&&noTerminator.Reason=="GetterKeyCStringUnterminatedOrOutsideCoveredDomain"&&!noTerminator.KeyLength.HasValue&&Released(noTerminator,empty),"65536 readable bytes without a terminator are not a complete C-string key");
  byte[] longSuffix=Repeated(65538,0x41);longSuffix[1]=0;var shortField=F("A","short");var shortTable=Table(shortField);var prefix=RecordTableGetter.GetValue(shortTable,longSuffix,fallback);
  Need(prefix.Status=="Known"&&prefix.KeyLength==1&&prefix.LocalKeyCapacity==15&&Object.ReferenceEquals(prefix.Value,shortField.Value)&&Released(prefix,shortTable),"the query budget applies to the C-string prefix rather than unread bytes after its terminator");
 }
 static void SnapshotRejections(){
  Need(ArgumentRejected(delegate{new TableString(null);})&&ArgumentRejected(delegate{new TableString(new byte[65537]);}),"unknown and oversized complete stored strings are rejected");
  Need(ArgumentRejected(delegate{new LoadTableField(null,S("value"));})&&ArgumentRejected(delegate{new LoadTableField(S("key"),null);}),"a field requires both complete key and complete value storage");
  Need(ArgumentRejected(delegate{new LoadTableSnapshot(new RegistrationGate(),null);})&&ArgumentRejected(delegate{Table(new LoadTableField[]{null});}),"a fixed snapshot cannot contain unknown field slots");
  Need(ArgumentRejected(delegate{Table(F("B","b"),F("A","a"));})&&ArgumentRejected(delegate{Table(F("A","one"),F("A","two"));}),"descending or duplicate complete keys are not valid unique ordered snapshots");
  Need(ArgumentRejected(delegate{Table(new LoadTableField(new TableString(new byte[]{0x80}),S("a")),new LoadTableField(new TableString(new byte[]{0x7f}),S("b")));}),"signed-char ordering cannot be substituted for unsigned-byte snapshot order");
  var many=new LoadTableField[4096];var emptyValue=S("");for(int i=0;i<many.Length;i++)many[i]=new LoadTableField(new TableString(new byte[]{(byte)(i>>8),(byte)(i&255)}),emptyValue);
  Need(Table(many).Count==4096&&ArgumentRejected(delegate{new LoadTableSnapshot(new RegistrationGate(),new LoadTableField[4097]);}),"the fixed field-count budget admits 4096 entries and rejects 4097");
  var sharedValue=new TableString(Repeated(65534,0x61));var exact=new LoadTableField[32];for(int i=0;i<exact.Length;i++)exact[i]=new LoadTableField(new TableString(new byte[]{(byte)(i+1),0x55}),sharedValue);
  var over=new LoadTableField[33];Array.Copy(exact,over,exact.Length);over[32]=new LoadTableField(new TableString(new byte[]{33,0x55}),S("x"));
  Need(Table(exact).Count==32&&ArgumentRejected(delegate{Table(over);}),"complete stored key/value payloads count toward the exact 2 MiB aggregate budget even when value identity is shared");
 }
 static int Main(){try{IdentityAndCString();LowerBoundAndUnsigned();SnapshotIsolation();GuardsAndBinary();LengthBoundaries();SnapshotRejections();Console.WriteLine("{\"status\":\"passed\",\"fixed_assertions\":"+n+",\"scope\":\"Own complete stable ordered LoadTable snapshots and the explicit absent-archive LoadTableBinary shortcut; no native tree layout, pointer cache, record parsing or original execution.\"}");return 0;}catch(Exception e){Console.Error.WriteLine(e);return 1;}}
}
