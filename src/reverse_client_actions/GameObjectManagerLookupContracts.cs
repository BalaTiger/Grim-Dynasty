using System;
using System.Collections.Generic;
using System.Threading;

namespace GrimDynasty.ReverseClient.Actions {
 // Explicit OWN platform synchronization, not recovered GAME::CriticalSection
 // storage or proof of Engine Enter/Exit wrapper bodies. Monitor supplies the
 // actual reentrant exclusion; diagnostic counters never substitute for it.
 public sealed class GameObjectManagerOwnCriticalSection {
  public readonly object Identity;
  int outstanding;
  public GameObjectManagerOwnCriticalSection():this(new object()){}
  public GameObjectManagerOwnCriticalSection(object identity){if(identity==null)throw new ArgumentNullException("identity");Identity=identity;}
  public int OwnOutstandingEnters{get{return Interlocked.CompareExchange(ref outstanding,0,0);}}
  internal void Enter(){Monitor.Enter(Identity);Interlocked.Increment(ref outstanding);}
  internal void Exit(){Monitor.Exit(Identity);Interlocked.Decrement(ref outstanding);}
 }
 public sealed class GameObjectManagerNode {
  public readonly Field<GameObjectManagerNode> Next00=new Field<GameObjectManagerNode>();
  public readonly Field<uint> Key10=new Field<uint>();
  public readonly Field<object> Object18=new Field<object>();
  bool released;
  public void MarkReleased(){released=true;}
  internal void RequireLive(uint at){if(released)throw new MissingContract(at,"Released own ObjectManager node");}
 }
 public sealed class GameObjectManagerBucket {
  public readonly Field<GameObjectManagerNode> First00=new Field<GameObjectManagerNode>(),Last08=new Field<GameObjectManagerNode>();
 }
 public sealed class GameObjectManagerBuckets {
  public readonly GameObjectManagerBucket[] Cells;
  // Capacity is an explicit OWN extent; all first/last cells remain Unknown.
  public GameObjectManagerBuckets(int capacity){if(capacity<0)throw new ArgumentOutOfRangeException("capacity");Cells=new GameObjectManagerBucket[capacity];for(int n=0;n<capacity;n++)Cells[n]=new GameObjectManagerBucket();}
  internal GameObjectManagerBucket At(ulong index,uint at){if(index>=(ulong)Cells.Length)throw new MissingContract(at,"Bucket index outside declared OWN stride16 extent");var cell=Cells[(int)index];if(cell==null)throw new MissingContract(at,"Missing own bucket view");return cell;}
 }
 public sealed class GameObjectManagerState {
  // This field declares the platform adapter for the inline critical section.
  // Unknown by default: constructing a manager does not establish a lock/map.
  public readonly Field<GameObjectManagerOwnCriticalSection> OwnCritical18=new Field<GameObjectManagerOwnCriticalSection>();
  public readonly Field<GameObjectManagerNode> Sentinel38=new Field<GameObjectManagerNode>();
  public readonly Field<GameObjectManagerBuckets> Buckets48=new Field<GameObjectManagerBuckets>();
  public readonly Field<ulong> Mask60=new Field<ulong>();
 }
 public sealed class GameObjectManagerLookupInvocation {
  public readonly Invocation Native=new Invocation();
  public readonly Field<ulong> Hash=new Field<ulong>(),BucketIndex=new Field<ulong>();
  public readonly Field<object> Result=new Field<object>();
  public bool OwnLockEntered,OwnLockExited,OwnCleanupReleased,Returned;
  public int HashedBytes,VisitedNodes;
  internal void Reset(){Native.At=0;Hash.Forget();BucketIndex.Forget();Result.Forget();OwnLockEntered=false;OwnLockExited=false;OwnCleanupReleased=false;Returned=false;HashedBytes=0;VisitedNodes=0;}
 }
 // Game019D40, r1-game-reached-goal-dependencies-run1/07,08. Supported map
 // domain: explicit typed shared cells, OWN platform lock, stable live backing,
 // finite acyclic bucket traversal. No CLR Dictionary replaces native hashing
 // or sentinel logic. Native wrapper internals/ABI/EH are not established.
 public static class GameObjectManagerLookupContracts {
  static T Need<T>(T value,uint at)where T:class{if(value==null)throw new MissingContract(at,"Null own ObjectManager view");return value;}
  static GameObjectManagerNode Node(GameObjectManagerNode value,uint at){Need(value,at).RequireLive(at);return value;}
  static ulong HashFour(uint id,GameObjectManagerLookupInvocation call){
   const ulong prime=0x100000001b3UL;ulong hash=0xcbf29ce484222325UL;
   uint[] mulAt={0x019d8d,0x019d99,0x019da5,0x019dac};
   for(int n=0;n<4;n++){call.Native.At=mulAt[n];hash=unchecked((hash^(byte)(id>>(8*n)))*prime);call.HashedBytes++;}
   call.Hash.Write(hash);return hash;
  }
  static void Leave(GameObjectManagerState manager,GameObjectManagerOwnCriticalSection held,GameObjectManagerLookupInvocation call,uint at){
   call.Native.At=at;var current=manager.OwnCritical18.Read(at);
   if(current==null||!Object.ReferenceEquals(current.Identity,held.Identity))throw new MissingContract(at,"OWN critical-section identity changed before Exit");
   held.Exit();call.OwnLockExited=true;
  }
  public static object Lookup019D40(GameObjectManagerState manager,uint id,GameObjectManagerLookupInvocation call){return LookupBits019D40(manager,Field<uint>.Of(id),call).Read(call.Native.At);}
  // Opaque result bits allow a caller's later TEST to consume an Unknown object
  // value after the real lookup/Exit prefix has returned. Key MOVs likewise
  // capture before the first byte is consumed by the hash at019D81.
  public static Field<object> LookupBits019D40(GameObjectManagerState manager,Field<uint> idBits,GameObjectManagerLookupInvocation call){
   if(call==null)throw new ArgumentNullException("call");call.Reset();var i=call.Native;
   i.At=0x019d5f;Need(manager,i.At);var key=Need(idBits,0x019d53).Snapshot();
   GameObjectManagerOwnCriticalSection held=null;
   try{
    i.At=0x019d63;held=Need(manager.OwnCritical18.Read(i.At),i.At);held.Enter();call.OwnLockEntered=true;
    i.At=0x019d69;var bucketBits=manager.Buckets48.Snapshot();
    i.At=0x019d81;uint id=key.Read(i.At);ulong hash=HashFour(id,call);
    i.At=0x019db0;var maskBits=manager.Mask60.Snapshot();
    i.At=0x019db4;ulong index=maskBits.Read(i.At)&hash;call.BucketIndex.Write(index);
    i.At=0x019db7;var sentinel=manager.Sentinel38.Snapshot();
    i.At=0x019dc4;var bucket=Need(bucketBits.Read(i.At),i.At).At(index,i.At);var cursor=bucket.First00.Snapshot();
    i.At=0x019dc8;var capturedFirst=bucket.First00.Snapshot();
    var visited=new HashSet<GameObjectManagerNode>();Field<GameObjectManagerNode> selected;
    for(;;){
     i.At=0x019dd0;bool empty=Object.ReferenceEquals(capturedFirst.Read(i.At),sentinel.Read(i.At));
     Field<GameObjectManagerNode> end;
     if(empty){i.At=0x019dd5;end=sentinel.Snapshot();}
     else {i.At=0x019dda;var lastBits=bucket.Last08.Snapshot();i.At=0x019dde;end=Node(lastBits.Read(i.At),i.At).Next00.Snapshot();}
     i.At=0x019de1;if(Object.ReferenceEquals(cursor.Read(i.At),end.Read(i.At))){i.At=0x019df6;selected=sentinel.Snapshot();break;}
     i.At=0x019de6;var current=Node(cursor.Read(i.At),i.At);
     if(!visited.Add(current))throw new MissingContract(i.At,"Cyclic own ObjectManager bucket; native nontermination is outside the supported domain");
     call.VisitedNodes++;
     if(current.Key10.Read(i.At)==id){i.At=0x019df0;selected=cursor.Snapshot();break;}
     i.At=0x019deb;cursor=current.Next00.Snapshot();
    }
    i.At=0x019dfd;Field<object> result;
    if(!Object.ReferenceEquals(selected.Read(i.At),sentinel.Read(i.At))){
     i.At=0x019e02;result=Node(selected.Read(i.At),i.At).Object18.Snapshot();
     Leave(manager,held,call,0x019e06);i.At=0x019e1e;
    }else{
     Leave(manager,held,call,0x019e1f);i.At=0x019e2a;result=Field<object>.Of(null);i.At=0x019e36;
    }
    call.Result.CopyFrom(result);call.Returned=true;return result;
   }finally{
    // This is OWN exceptional cleanup only, not invented native Exit/EH.
    if(call.OwnLockEntered&&!call.OwnLockExited){held.Exit();call.OwnCleanupReleased=true;}
   }
  }
 }
}
