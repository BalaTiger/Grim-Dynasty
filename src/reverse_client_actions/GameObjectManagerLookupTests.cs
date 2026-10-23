using System;
using System.Collections.Generic;
using GrimDynasty.ReverseClient.Actions;

// New019D40 OWN map tests. The synchronization adapter uses real Monitor;
// these cases do not establish the native6090C8/6090C0 wrapper implementations.
public static class GameObjectManagerLookupTests {
 static int scenarios,assertions;
 public static int Scenarios{get{return scenarios;}}
 public static int Assertions{get{return assertions;}}
 static void Check(bool value,string why){assertions++;if(!value)throw new Exception(why);}
 static void Equal<T>(T expected,T actual,string why){Check(EqualityComparer<T>.Default.Equals(expected,actual),why+" expected="+expected+" actual="+actual);}
 static void Test(string name,Action body){body();scenarios++;Console.WriteLine("PASS "+name);}
 static void Missing(uint at,Action body,string why){try{body();}catch(MissingContract e){Equal(at,e.Address,why);return;}throw new Exception("Expected unsupported observation: "+why);}
 static void Unknown<T>(Field<T> field,string why){Missing(0,delegate{field.Read(0);},why);}
 sealed class OwnFixture {
  public readonly GameObjectManagerState Manager=new GameObjectManagerState();
  public readonly GameObjectManagerOwnCriticalSection Critical=new GameObjectManagerOwnCriticalSection();
  public readonly GameObjectManagerNode Sentinel=new GameObjectManagerNode();
  public readonly GameObjectManagerBuckets Buckets=new GameObjectManagerBuckets(8);
  public readonly GameObjectManagerLookupInvocation Call=new GameObjectManagerLookupInvocation();
  public OwnFixture(){Manager.OwnCritical18.Write(Critical);Manager.Sentinel38.Write(Sentinel);Manager.Buckets48.Write(Buckets);Manager.Mask60.Write(7);}
  public void Empty(int bucket){Buckets.Cells[bucket].First00.Write(Sentinel);}
  public GameObjectManagerNode Single(int bucket,uint key){
   var node=new GameObjectManagerNode();node.Key10.Write(key);node.Next00.Write(Sentinel);Buckets.Cells[bucket].First00.Write(node);Buckets.Cells[bucket].Last08.Write(node);return node;
  }
  public object Lookup(uint key){return GameObjectManagerLookupContracts.Lookup019D40(Manager,key,Call);}
  public void Returned(uint at,int visited){
   Equal(at,Call.Native.At,"Native return point");Check(Call.Returned,"Native lookup completed");Check(Call.OwnLockEntered,"OWN Monitor entered");Check(Call.OwnLockExited,"Normal OWN Monitor exit");Check(!Call.OwnCleanupReleased,"No exceptional cleanup on normal return");Equal(0,Critical.OwnOutstandingEnters,"No outstanding own lock enter");Equal(visited,Call.VisitedNodes,"Exact visited node count");
  }
  public void Failed(uint at,Action action){
   Missing(at,action,"Exact unsupported read");Equal(at,Call.Native.At,"Invocation stopped at read");Check(!Call.Returned,"No fabricated return");Check(Call.OwnLockEntered,"Failure follows OWN Monitor enter");Check(!Call.OwnLockExited,"Normal Exit was not reached");Check(Call.OwnCleanupReleased,"OWN exceptional cleanup released Monitor");Equal(0,Critical.OwnOutstandingEnters,"Exceptional prefix leaves no lock held");Unknown(Call.Result,"No fabricated result after unsupported prefix");
  }
 }
 public static void Run(){
  scenarios=0;assertions=0;
  Test("Four little-endian bytes produce independently fixed FNV64 values including unsigned high bits",delegate{
   // Independent byte-vector reference values; no production hash helper is
   // called to prepare either the expected hash or selected bucket.
   uint[] ids={0x00000000,0x01020304,0xffffffff,0x80000000,0x00000001,0x00000100,0x00010000,0x01000000};
   ulong[] hashes={0x4d25767f9dce13f5UL,0xb345225e3644edb5UL,0x994f76653e2a3951UL,0x4d24f67f9dcd3a75UL,0xad2aca7747985764UL,0x447bfb7f98e6154aUL,0x4d22107f9dcb30ccUL,0x4d25757f9dce1242UL};
   int[] buckets={5,5,1,5,4,2,4,2};
   for(int n=0;n<ids.Length;n++){
    var f=new OwnFixture();f.Empty(buckets[n]);Check(f.Lookup(ids[n])==null,"Explicit empty map miss");
    Equal(hashes[n],f.Call.Hash.Read(0),"FNV reference"+n);Equal((ulong)buckets[n],f.Call.BucketIndex.Read(0),"Selected exact bucket"+n);Equal(4,f.Call.HashedBytes,"Exactly four bytes including leading zero bytes");f.Returned(0x019e36,0);
   }
  });
  Test("Shared collision chain finds middle and unsigned last key and terminates at last.Next on miss",delegate{
   var f=new OwnFixture();var a=new GameObjectManagerNode();var b=new GameObjectManagerNode();var c=new GameObjectManagerNode();var after=new GameObjectManagerNode();
   a.Key10.Write(0);b.Key10.Write(0x01020304);c.Key10.Write(0x80000000);a.Next00.Write(b);b.Next00.Write(c);c.Next00.Write(after);
   f.Buckets.Cells[5].First00.Write(a);f.Buckets.Cells[5].Last08.Write(c);var middle=new object();var last=new object();b.Object18.Write(middle);c.Object18.Write(last);
   Check(Object.ReferenceEquals(middle,f.Lookup(0x01020304)),"Actual middle node object identity");f.Returned(0x019e1e,2);
   Check(Object.ReferenceEquals(last,f.Lookup(0x80000000)),"Unsigned high-bit last node match");f.Returned(0x019e1e,3);
   Check(f.Lookup(8)==null,"Missing colliding key reaches last.Next");Equal(5UL,f.Call.BucketIndex.Read(0),"Independent key8 collision");f.Returned(0x019e36,3);
   Unknown(after.Key10,"Exclusive last.Next boundary never inspected");Unknown(after.Next00,"Boundary is not followed");Unknown(a.Object18,"Nonmatching node object never read");
   var high=f.Single(1,0xffffffff);var highObject=new object();high.Object18.Write(highObject);Check(Object.ReferenceEquals(highObject,f.Lookup(0xffffffff)),"UInt32 maximum compares without signed narrowing");f.Returned(0x019e1e,1);
  });
  Test("Explicit empty first equals sentinel without requiring last or sentinel node fields",delegate{
   var f=new OwnFixture();f.Empty(5);Check(f.Lookup(0)==null,"Empty bucket returns null");f.Returned(0x019e36,0);
   Unknown(f.Buckets.Cells[5].Last08,"Empty branch never reads last");Unknown(f.Sentinel.Next00,"Empty sentinel not dereferenced");Unknown(f.Sentinel.Key10,"Empty sentinel key not consumed");Unknown(f.Sentinel.Object18,"Empty sentinel value not consumed");
   var fresh=new GameObjectManagerState();Unknown(fresh.OwnCritical18,"Manager constructor does not create a lock");Unknown(fresh.Sentinel38,"Manager constructor does not invent sentinel");Unknown(fresh.Buckets48,"Manager constructor does not invent buckets");Unknown(fresh.Mask60,"Manager constructor does not invent mask");
  });
  Test("Unknown lock fails before hashing and reused invocation never retains an earlier successful result",delegate{
   var f=new OwnFixture();var node=f.Single(5,0);var value=new object();node.Object18.Write(value);Check(Object.ReferenceEquals(value,f.Lookup(0)),"Initial lookup succeeds");f.Returned(0x019e1e,1);
   f.Manager.OwnCritical18.Forget();Missing(0x019d63,delegate{f.Lookup(0);},"Unknown lock is not a default successful adapter");Equal(0x019d63u,f.Call.Native.At,"Lock call-site stop");
   Check(!f.Call.Returned,"Old success reset");Check(!f.Call.OwnLockEntered&&!f.Call.OwnLockExited&&!f.Call.OwnCleanupReleased,"No lock or cleanup fabricated before enter");Equal(0,f.Call.HashedBytes,"Hash counter reset");Equal(0,f.Call.VisitedNodes,"Traversal counter reset");Unknown(f.Call.Hash,"Old hash forgotten");Unknown(f.Call.BucketIndex,"Old bucket forgotten");Unknown(f.Call.Result,"Old object result forgotten");Equal(0,f.Critical.OwnOutstandingEnters,"Prior successful lock is released");
   f.Manager.OwnCritical18.Write(null);Missing(0x019d63,delegate{f.Lookup(0);},"Known null lock also unsupported");Check(!f.Call.OwnCleanupReleased,"No cleanup claim for absent lock");
  });
  Test("Unknown key mask bucket sentinel last next and node reads preserve order and release the OWN lock",delegate{
   var f=new OwnFixture();f.Single(5,0);var id=new Field<uint>();
   f.Failed(0x019d81,delegate{GameObjectManagerLookupContracts.LookupBits019D40(f.Manager,id,f.Call);});Equal(0,f.Call.HashedBytes,"Input key consumed before first hash multiplication");Unknown(f.Call.Hash,"No complete hash from unknown key");
   f=new OwnFixture();f.Manager.Buckets48.Forget();f.Manager.Mask60.Forget();f.Failed(0x019db4,delegate{f.Lookup(0);});Equal(4,f.Call.HashedBytes,"Full hash precedes mask consumption");Equal(0x4d25767f9dce13f5UL,f.Call.Hash.Read(0),"Hash prefix retained");Unknown(f.Call.BucketIndex,"No index from unknown mask");
   f.Manager.Mask60.Write(7);f.Failed(0x019dc4,delegate{f.Lookup(0);});Equal(5UL,f.Call.BucketIndex.Read(0),"Index precedes captured bucket pointer use");
   f=new OwnFixture();f.Single(5,0);f.Manager.Sentinel38.Forget();f.Failed(0x019dd0,delegate{f.Lookup(0);});
   f=new OwnFixture();f.Failed(0x019dd0,delegate{f.Lookup(0);});
   f=new OwnFixture();var node=f.Single(5,0);f.Buckets.Cells[5].Last08.Forget();f.Failed(0x019dde,delegate{f.Lookup(0);});
   f.Buckets.Cells[5].Last08.Write(node);node.Next00.Forget();f.Failed(0x019de1,delegate{f.Lookup(0);});Equal(0,f.Call.VisitedNodes,"End is consumed before any node key");
   node.Next00.Write(f.Sentinel);node.Key10.Forget();f.Failed(0x019de6,delegate{f.Lookup(0);});Equal(1,f.Call.VisitedNodes,"First actual key inspection counted");
   f=new OwnFixture();var first=new GameObjectManagerNode();var last=new GameObjectManagerNode();first.Key10.Write(8);last.Next00.Write(f.Sentinel);f.Buckets.Cells[5].First00.Write(first);f.Buckets.Cells[5].Last08.Write(last);
   f.Failed(0x019de1,delegate{f.Lookup(0);});Equal(1,f.Call.VisitedNodes,"Unknown Next preserved until following comparison");
  });
  Test("Unknown object MOV returns opaque bits after normal Exit and only the caller consumes their value",delegate{
   var f=new OwnFixture();var node=f.Single(5,0);var result=GameObjectManagerLookupContracts.LookupBits019D40(f.Manager,Field<uint>.Of(0),f.Call);f.Returned(0x019e1e,1);
   Missing(0x0abcde,delegate{result.Read(0x0abcde);},"Caller consumes opaque unknown Object18");Unknown(f.Call.Result,"Diagnostic result preserves Unknown instead of null");
   Missing(0x019e1e,delegate{f.Lookup(0);},"Convenience API consumes value after actual return");f.Returned(0x019e1e,1);
   node.Object18.Write(null);Check(f.Lookup(0)==null,"A found known-null value stays known-null");f.Returned(0x019e1e,1);
   var value=new object();node.Object18.Write(value);Check(Object.ReferenceEquals(value,f.Lookup(0)),"Later lookup reads same actual node live value");Check(Object.ReferenceEquals(value,f.Call.Result.Read(0)),"Diagnostic result preserves object identity");f.Returned(0x019e1e,1);
  });
 }
}
public static class GameObjectManagerLookupTestProgram {
 public static int Main(){try{GameObjectManagerLookupTests.Run();Console.WriteLine("PASS "+GameObjectManagerLookupTests.Scenarios+" scenarios, "+GameObjectManagerLookupTests.Assertions+" assertions; new019D40 OWN map and platform Monitor contracts only");return 0;}catch(Exception error){Console.Error.WriteLine(error);return 1;}}
}
