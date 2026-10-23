using System;
using System.IO;
using GrimDynasty.Resources;

// Synthetic bytes only. Expected branches come from the archived 278D20
// entry/body instructions and copy tables, not from running the native DLL.
// Unsupported means this safe profile stopped; it is not a native error code.
class Lz4OutputSizeProfileTests {
 static int assertions,scenarios;
 static void Need(bool value,string why){assertions++;if(!value)throw new Exception(why);}
 static byte[] B(string text){var bytes=new byte[text.Length];for(int i=0;i<bytes.Length;i++)bytes[i]=checked((byte)text[i]);return bytes;}
 static bool Same(byte[] a,byte[] b){if(a.Length!=b.Length)return false;for(int i=0;i<a.Length;i++)if(a[i]!=b[i])return false;return true;}
 static byte[] Join(params byte[][] parts){
  using(var stream=new MemoryStream()){foreach(byte[] part in parts)stream.Write(part,0,part.Length);return stream.ToArray();}
 }
 static void LengthExtension(MemoryStream stream,int remaining){
  while(remaining>=255){stream.WriteByte(255);remaining-=255;}stream.WriteByte(checked((byte)remaining));
 }
 static byte[] Literal(byte[] bytes){
  using(var stream=new MemoryStream()){
   stream.WriteByte(checked((byte)(Math.Min(15,bytes.Length)<<4)));
   if(bytes.Length>=15)LengthExtension(stream,bytes.Length-15);
   stream.Write(bytes,0,bytes.Length);return stream.ToArray();
  }
 }
 static byte Seed(int index){return unchecked((byte)(index*37+11));}
 static byte[] Sequence(int distance,int matchLength,int tailLength){
  // One literal seed, a backward match, then exactly tailLength final literals.
  using(var stream=new MemoryStream()){
   stream.WriteByte(checked((byte)((Math.Min(15,distance)<<4)|Math.Min(15,matchLength-4))));
   if(distance>=15)LengthExtension(stream,distance-15);
   for(int i=0;i<distance;i++)stream.WriteByte(Seed(i));
   stream.WriteByte(unchecked((byte)distance));stream.WriteByte(unchecked((byte)(distance>>8)));
   if(matchLength>=19)LengthExtension(stream,matchLength-19);
   stream.WriteByte(checked((byte)(tailLength<<4)));
   for(int i=0;i<tailLength;i++)stream.WriteByte(checked((byte)(48+i)));
   return stream.ToArray();
  }
 }
 static byte[] RepeatedExpected(int distance,int matchLength,int tailLength){
  var bytes=new byte[distance+matchLength+tailLength];
  for(int i=0;i<distance+matchLength;i++)bytes[i]=Seed(i%distance);
  for(int i=0;i<tailLength;i++)bytes[distance+matchLength+i]=checked((byte)(48+i));
  return bytes;
 }
 static byte[] SeedPrefix(byte token,params byte[] suffix){return Join(new[]{token},B("ABCDEFGH"),suffix);}
 static Lz4OutputSizeResult Success(byte[] source,byte[] expected,int consumed,string why){
  var result=Lz4OutputSizeProfile.DecodeChecked(source,expected.Length);
  Need(result.Status==Lz4OutputSizeStatus.Success&&result.Reason==null&&
   result.InputPosition==consumed&&result.NativeReturn==consumed&&Same(result.CopyOutput(),expected),why);
  return result;
 }
 static void Failure(byte[] source,int expected,Lz4OutputSizeStatus status,string reason,int position,int? nativeReturn,string why){
  var result=Lz4OutputSizeProfile.DecodeChecked(source,expected);bool hidden=false;
  try{result.CopyOutput();}catch(InvalidOperationException){hidden=true;}
  Need(result.Status==status&&result.Reason==reason&&result.InputPosition==position&&result.NativeReturn==nativeReturn&&hidden,why);
 }
 static void Unsupported(byte[] source,int expected,string reason,int position,string why){
  Failure(source,expected,Lz4OutputSizeStatus.Unsupported,reason,position,null,why);
 }
 static void Rejected(byte[] source,int expected,string reason,int position,string why){
  Failure(source,expected,Lz4OutputSizeStatus.NativeLengthRejected,reason,position,-position-1,why);
 }
 static void ZeroAndBudgets(){
  Need(Lz4OutputSizeProfile.MaxInputBytes==2097152&&Lz4OutputSizeProfile.MaxOutputBytes==1048576,"budgets are explicit own profile limits");
  Success(new byte[]{0},new byte[0],1,"zero output consumes its required zero token");
  Success(new byte[]{0,0xff,0x7f},new byte[0],1,"zero output does not consume trailing bytes");
  Rejected(new byte[]{1,0},0,"NonzeroTokenForZeroOutput",0,"zero-output nonzero token returns -1 without advancing the cursor");
  Unsupported(new byte[0],0,"MissingZeroOutputToken",0,"empty source cannot supply the native zero-output byte read");
  Unsupported(null,1,"NullSource",0,"null source is a coverage rejection, not a fabricated native return");
  Unsupported(new byte[0],1,"MissingToken",0,"nonzero output requires a first token");
  Unsupported(new byte[]{0},-1,"OutputSizeOutsideCoveredDomain",0,"negative output length is outside the safe profile");
  Unsupported(new byte[]{0},Lz4OutputSizeProfile.MaxOutputBytes+1,"OutputSizeOutsideCoveredDomain",0,"one byte beyond the output budget is rejected before decoding");
  Unsupported(new byte[Lz4OutputSizeProfile.MaxInputBytes+1],0,"InputBytesOutsideCoveredDomain",0,"input budget applies even when the first byte could finish zero output");
  Success(new byte[Lz4OutputSizeProfile.MaxInputBytes],new byte[0],1,"the exact input budget is permitted and its unused suffix is ignored");
 }
 static void LiteralBoundaries(){
  int[] lengths={1,7,8,14,15,16,270,271};
  foreach(int length in lengths){
   var bytes=new byte[length];for(int i=0;i<bytes.Length;i++)bytes[i]=Seed(i);
   byte[] source=Literal(bytes);Success(source,bytes,source.Length,"final literal and 15/255 extension boundaries preserve exact bytes and consumed input");
  }
  var largest=new byte[Lz4OutputSizeProfile.MaxOutputBytes];largest[0]=0xa5;largest[largest.Length-1]=0x5a;
  byte[] largestSource=Literal(largest);
  Success(largestSource,largest,largestSource.Length,"the exact output budget is supported by a complete literal block");
  byte[] small=Literal(B("abc")),before=(byte[])small.Clone();
  var result=Success(small,B("abc"),4,"small final literal completes at the output end");
  Need(Same(small,before),"decoding leaves its source bytes unchanged");
  byte[] exported=result.CopyOutput();exported[0]=0;small[1]=0;
  Need(Same(result.CopyOutput(),B("abc"))&&!Object.ReferenceEquals(exported,result.CopyOutput()),"successful output is detached from source and every export is a new clone");
 }
 static void LiteralFailures(){
  Rejected(new byte[]{0x20,65,66},3,"LiteralEndDoesNotEqualOutputEnd",1,"short final literal is a native length rejection before source copying");
  Rejected(new byte[]{0x40,65,66,67,68},3,"LiteralEndDoesNotEqualOutputEnd",1,"oversized final literal uses the token cursor in its native error code");
  Rejected(new byte[]{0xf0,0},16,"LiteralEndDoesNotEqualOutputEnd",2,"complete literal extension is consumed before the output-end comparison");
  Rejected(new byte[]{0xf0,255,0},269,"LiteralEndDoesNotEqualOutputEnd",3,"all literal extension bytes are consumed instead of clamping early to expected");
  Unsupported(new byte[]{0xf0},15,"TruncatedLiteralLength",1,"missing first literal extension stops at the consumed token");
  Unsupported(new byte[]{0xf0,255},270,"TruncatedLiteralLength",2,"missing continued literal extension preserves the consumed extension cursor");
  Unsupported(new byte[]{0x30,65,66},3,"TruncatedFinalLiteral",1,"exact final length with unreadable source is a safe-profile stop");
  Unsupported(Join(new byte[]{0xf0,0},new byte[14]),15,"TruncatedFinalLiteral",2,"a final literal's source check follows all extension bytes");
 }
 static void MatchDistancesAndThresholds(){
  // Distances 1..7 exercise every nonzero small-distance table entry; 8/9
  // cross the qword-copy branch and 256/257 exercise both offset bytes.
  int[] distances={1,2,3,4,5,6,7,8,9,256,257};
  foreach(int distance in distances){
   byte[] source=Sequence(distance,20,5);
   Success(source,RepeatedExpected(distance,20,5),source.Length,"valid backward distance reproduces history/overlap and accepts matchEnd == end-5");
  }
  foreach(int length in new[]{274,275}){
   byte[] source=Sequence(8,length,5);
   Success(source,RepeatedExpected(8,length,5),source.Length,"match extensions spanning 255 preserve output and consumed cursor");
  }
  foreach(int tail in new[]{11,12,13}){
   byte[] source=Sequence(9,4,tail);
   Success(source,RepeatedExpected(9,4,tail),source.Length,"matchEnd on either side of end-12 selects copying strategy rather than rejecting length");
  }
 }
 static void ZeroLiteralScenario(){
  byte[] source=Join(new byte[]{0x80},B("ABCDEFGH"),new byte[]{8,0,0,4,0,0x50},B("12345"));
  byte[] before=(byte[])source.Clone();
  Success(source,B("ABCDEFGHABCDABCD12345"),20,"two actual match sequences include a zero-literal second sequence followed by final literals");
  Need(Same(source,before),"multi-sequence decoding does not mutate encoded history or lookahead bytes");
  var truncated=new byte[source.Length-1];Array.Copy(source,truncated,truncated.Length);
  Unsupported(truncated,21,"InsufficientLiteralWildCopySource",12,"zero logical literals still require eight readable source bytes at the second sequence");
  scenarios++;
 }
 static void NativeBoundaries(){
  Rejected(Sequence(1,4,4),9,"MatchEndExceedsLastLiteralsBoundary",4,"literalEnd == end-8 enters the match branch, which rejects fewer than five final literals");
  Rejected(Sequence(1,4,3),8,"LiteralEndDoesNotEqualOutputEnd",1,"literalEnd > end-8 reaches the final-literal comparison without reading the offset");
  Rejected(SeedPrefix(0x8f,1,0,255,0),286,"MatchEndExceedsLastLiteralsBoundary",13,"native match rejection uses the cursor after the complete 255/0 extension");
  byte[] complete=Sequence(9,4,5);var shortLookahead=new byte[13];Array.Copy(complete,shortLookahead,shortLookahead.Length);
  Unsupported(shortLookahead,18,"InsufficientLiteralWildCopySource",1,"nine logical literals require sixteen readable bytes for native qword lookahead");
 }
 static void InvalidSafetyDomain(){
  Unsupported(SeedPrefix(0x80,1),17,"TruncatedOffset",9,"a one-byte offset is not partly consumed");
  Unsupported(SeedPrefix(0x80,0,0),17,"InvalidMatchDistance",11,"offset zero is a coverage stop after reading its complete word");
  Unsupported(SeedPrefix(0x80,9,0),17,"InvalidMatchDistance",11,"an offset before produced history is not treated as a native length error");
  Unsupported(new byte[]{0,1,0,0,0,0,0,0,0},9,"InvalidMatchDistance",3,"readable wildcard source cannot supply missing output history");
  Unsupported(SeedPrefix(0x8f,1,0),32,"TruncatedMatchLength",11,"match extension is required after the complete offset");
  Unsupported(SeedPrefix(0x8f,1,0,255),300,"TruncatedMatchLength",12,"continued match extension stops at its actual consumed cursor");
  Unsupported(SeedPrefix(0x80,8,0),17,"MissingToken",11,"a valid partial match with no following token exposes no partial output");
 }
 static void OldDecoderDifference(){
  byte[] oldMatchEnding={0x14,97,1,0};
  Need(Same(Lz4Block.Decode(oldMatchEnding,9),B("aaaaaaaaa")),"existing input-bounded decoder still supports its historical match-ending fixture");
  Unsupported(oldMatchEnding,9,"InsufficientLiteralWildCopySource",1,"the same short fixture does not establish readable native wildcard input");
  Rejected(Join(oldMatchEnding,new byte[5]),9,"MatchEndExceedsLastLiteralsBoundary",4,"padding makes source reads safe but does not remove the native last-literals restriction");
  Need(Lz4Block.Decode(new byte[0],0).Length==0,"the old empty-input/zero-output contract remains explicitly distinct");
 }
 static void ConsumedInputScenario(){
  byte[] first=Literal(B("abc")),second=Literal(B("de")),combined=Join(first,second);
  var one=Success(combined,B("abc"),4,"output-size termination accepts a following encoded block without consuming it");
  var remainder=new byte[combined.Length-one.InputPosition];Array.Copy(combined,one.InputPosition,remainder,0,remainder.Length);
  Success(remainder,B("de"),3,"the returned consumed cursor locates the second complete block");
  bool oldRejected=false;try{Lz4Block.Decode(combined,3);}catch(InvalidDataException){oldRejected=true;}
  Need(oldRejected,"the old input-bounded decoder does not silently adopt the output-size trailing-input contract");
  scenarios++;
 }
 public static int Main(){
  try{
   ZeroAndBudgets();LiteralBoundaries();LiteralFailures();MatchDistancesAndThresholds();
   ZeroLiteralScenario();NativeBoundaries();InvalidSafetyDomain();OldDecoderDifference();ConsumedInputScenario();
   Console.WriteLine("{\"status\":\"passed\",\"fixed_assertions\":"+assertions+",\"scenarios\":"+scenarios+
    ",\"scope\":\"synthetic bounded output-size profile; no native execution or game samples\"}");return 0;
  }catch(Exception error){Console.Error.WriteLine(error.GetType().Name+": "+error.Message);return 1;}
 }
}
