using System;
using System.Collections.Generic;
using GrimDynasty.ReverseClient.Actions;

// New533410 tests only. OWN empty ranges are explicit. There is no successful
// fake skill callback and no claim that a native loaded SkillManager is empty.
public static class SkillMovedTests {
 static int scenarios,assertions;
 public static int Scenarios{get{return scenarios;}}
 public static int Assertions{get{return assertions;}}
 static void Check(bool value,string why){assertions++;if(!value)throw new Exception(why);}
 static void Equal<T>(T expected,T actual,string why){Check(EqualityComparer<T>.Default.Equals(expected,actual),why+" expected="+expected+" actual="+actual);}
 static void Test(string name,Action body){body();scenarios++;Console.WriteLine("PASS "+name);}
 static void Missing(uint at,Action body,string why){try{body();}catch(MissingContract e){Equal(at,e.Address,why);return;}throw new Exception("Expected unsupported observation: "+why);}
 static void Unknown<T>(Field<T> field,string why){Missing(0,delegate{field.Read(0);},why);}
 static SkillMovedManagerState Fresh(){return SkillMovedManagerState.ForOwner(new GameMovementCharacterState(new EntityMovementState(new object())));}
 static NavigationCrowdPointerVector<SkillMovedTarget>[] Vectors(SkillMovedManagerState m){return new[]{m.Collections228,m.Collections258,m.Collections360};}
 static void Run(SkillMovedManagerState m){SkillMovedContracts.Moved533410(m,new Invocation());}
 public static void Run(){
  scenarios=0;assertions=0;
  Test("Each canonical Game owner retains exactly one SkillManager and all headers start unknown",delegate{
   var m=Fresh();Check(Object.ReferenceEquals(m,SkillMovedManagerState.ForOwner(m.Owner)),"Same owner manager identity");
   Check(!Object.ReferenceEquals(m,Fresh()),"Distinct owners remain distinct");
   Check(Object.ReferenceEquals(m.Owner.Entity,m.Owner.Interface38.FullOwner.Entity),"Canonical Entity and interface identity");
   foreach(var v in Vectors(m)){Unknown(v.Begin00,"Begin not defaulted");Unknown(v.End08,"End not defaulted");Unknown(v.Capacity10,"Capacity not defaulted");}
   Missing(0x533424,delegate{Run(m);},"Opaque begin MOV is consumed by first comparison");
  });
  Test("Explicit null empty collections finish without reading capacity or unrelated Game fields",delegate{
   var m=Fresh();m.UseOwnEmptyCollections();var i=new Invocation();SkillMovedContracts.Moved533410(m,i);Equal(0x5334b3u,i.At,"Actual return address");
   foreach(var v in Vectors(m)){Check(v.Begin00.Read(0)==null,"Empty begin retained");Check(v.End08.Read(0)==null,"Empty end retained");Unknown(v.Capacity10,"Unconsumed capacity remains unknown");}
   Unknown(m.Owner.Entity.Virtual00,"Primary Game table not read");Unknown(m.Owner.ActionState24B4,"Game action not read");
  });
  Test("Pointer equality accepts allocated empty extents without dereferencing storage",delegate{
   var m=Fresh();foreach(var v in Vectors(m)){var buffer=new NavigationCrowdPointerBuffer<SkillMovedTarget>(2);v.Begin00.Write(new NavigationCrowdPointerAddress<SkillMovedTarget>(buffer,1));v.End08.Write(new NavigationCrowdPointerAddress<SkillMovedTarget>(buffer,1));buffer.MarkReleased();}
   var i=new Invocation();SkillMovedContracts.Moved533410(m,i);Equal(0x5334b3u,i.At,"Equal borrowed values need no element dereference");
  });
  Test("Sequential unknown end and begin consumption occurs at each real comparison",delegate{
   var compare=new uint[]{0x533424,0x533450,0x533480};
   for(int n=0;n<3;n++){
    var m=Fresh();m.UseOwnEmptyCollections();var v=Vectors(m)[n];v.Begin00.Forget();Missing(compare[n],delegate{Run(m);},"Begin snapshot use"+n);
    v.Begin00.Write(null);v.End08.Forget();Missing(compare[n],delegate{Run(m);},"End live use"+n);
   }
  });
  Test("All three nonempty ranges stop at their exact unbound virtual call before later ranges",delegate{
   var call=new uint[]{0x533436,0x533466,0x533496};
   for(int n=0;n<3;n++){
    var m=Fresh();m.UseOwnEmptyCollections();var vectors=Vectors(m);var buffer=new NavigationCrowdPointerBuffer<SkillMovedTarget>(1);var target=new SkillMovedTarget();target.Virtual00.Write(new object());buffer.At(0,0).Write(target);vectors[n].InstallOwnStorage(buffer,1);
    if(n<2)vectors[n+1].Begin00.Forget();var i=new Invocation();
    Missing(call[n],delegate{SkillMovedContracts.Moved533410(m,i);},"No target inference at collection"+n);Equal(call[n],i.At,"Invocation at unsupported slot"+n);
    Check(Object.ReferenceEquals(target,buffer.At(0,0).Read(0)),"No element mutation"+n);Equal(0,vectors[n].Begin00.Read(0).Index,"No cursor publication"+n);Equal(1,vectors[n].End08.Read(0).Index,"End untouched"+n);
   }
  });
  Test("Element address, target identity and vptr are consumed at their separate native stages",delegate{
   var load=new uint[]{0x533430,0x533460,0x533490};var vptr=new uint[]{0x533433,0x533463,0x533493};var call=new uint[]{0x533436,0x533466,0x533496};
   for(int n=0;n<3;n++){
    var m=Fresh();m.UseOwnEmptyCollections();var v=Vectors(m)[n];var buffer=new NavigationCrowdPointerBuffer<SkillMovedTarget>(1);v.Begin00.Write(null);v.End08.Write(new NavigationCrowdPointerAddress<SkillMovedTarget>(buffer,1));
    Missing(load[n],delegate{Run(m);},"Null element address"+n);v.Begin00.Write(new NavigationCrowdPointerAddress<SkillMovedTarget>(buffer,0));
    Missing(vptr[n],delegate{Run(m);},"Unknown target consumed as address"+n);buffer.At(0,0).Write(null);Missing(vptr[n],delegate{Run(m);},"Known null target"+n);
    var target=new SkillMovedTarget();buffer.At(0,0).Write(target);Missing(call[n],delegate{Run(m);},"Unknown vptr consumed at call"+n);target.Virtual00.Write(null);Missing(call[n],delegate{Run(m);},"Known null vptr is not success"+n);
    target.MarkReleased();Missing(vptr[n],delegate{Run(m);},"Released target at vptr load"+n);buffer.MarkReleased();Missing(load[n],delegate{Run(m);},"Released element storage"+n);
   }
  });
  Test("Range equality uses allocation identity and malformed nonempty extents are not emptied",delegate{
   var m=Fresh();m.UseOwnEmptyCollections();var a=new NavigationCrowdPointerBuffer<SkillMovedTarget>(1);var b=new NavigationCrowdPointerBuffer<SkillMovedTarget>(1);
   m.Collections228.Begin00.Write(new NavigationCrowdPointerAddress<SkillMovedTarget>(a,1));m.Collections228.End08.Write(new NavigationCrowdPointerAddress<SkillMovedTarget>(b,1));
   Missing(0x533430,delegate{Run(m);},"Distinct buffers at same index are distinct pointers");
   m.Collections228.End08.Write(new NavigationCrowdPointerAddress<SkillMovedTarget>(a,0));Missing(0x533430,delegate{Run(m);},"Reversed range dereferences original begin instead of being treated as empty");
  });
  Test("Released canonical owner and invalid entry inputs stop before header access",delegate{
   var m=Fresh();m.UseOwnEmptyCollections();m.Owner.Entity.MarkReleased();Missing(0x53341a,delegate{Run(m);},"Embedded manager shares owner lifetime");
   Missing(0x53341a,delegate{SkillMovedContracts.Moved533410(null,new Invocation());},"Null manager");Missing(0x53341a,delegate{SkillMovedContracts.Moved533410(Fresh(),null);},"Null invocation");
  });
 }
}
public static class SkillMovedTestProgram {
 public static int Main(){try{SkillMovedTests.Run();Console.WriteLine("PASS "+SkillMovedTests.Scenarios+" scenarios, "+SkillMovedTests.Assertions+" assertions; new533410 OWN empty and exact unsupported contracts only");return 0;}catch(Exception error){Console.Error.WriteLine(error);return 1;}}
}
