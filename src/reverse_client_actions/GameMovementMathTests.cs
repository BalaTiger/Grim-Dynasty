using System;
using GrimDynasty.ReverseClient.Actions;

// New isolated mathematical contracts only. No native function, scene, real
// mouse, loaded state or old test executable is invoked by this fixture.
public static class GameMovementMathTests {
 static int scenarios,assertions;
 static void Check(bool value,string message){assertions++;if(!value)throw new Exception(message);}
 static uint Bits(float x){return BitConverter.ToUInt32(BitConverter.GetBytes(x),0);}
 static void Equal(float want,float actual,string message){Check(Bits(want)==Bits(actual),message+" want="+want+" got="+actual);}
 static void Same(object a,object b,string message){Check(Object.ReferenceEquals(a,b),message);}
 static void Test(string name,Action test){test();scenarios++;Console.WriteLine("PASS "+name);}
 static void Missing(uint at,Action body,string message){try{body();}catch(MissingContract e){Check(e.Address==at,message+" address="+e.Address.ToString("x"));return;}throw new Exception("Expected Unsupported: "+message);}
 static NavigationPoint3 Point(float x,float y,float z){return new NavigationPoint3(Field<float>.Of(x),Field<float>.Of(y),Field<float>.Of(z));}
 static GameMovementMathConstants Constants(){var c=new GameMovementMathConstants();c.UseObservedDiskOne();return c;}
 static GameMovementMatrix48 Matrix(float value){var m=new GameMovementMatrix48();for(int n=0;n<12;n++)m.At(n,0).Write(value);return m;}
 static GameMovementMatrix48 Values(params float[] values){if(values.Length!=12)throw new Exception("Bad fixture extent");var m=new GameMovementMatrix48();for(int n=0;n<12;n++)m.At(n,0).Write(values[n]);return m;}
 static GameMovementMatrix48 Sequence(int first){var m=new GameMovementMatrix48();for(int n=0;n<12;n++)m.At(n,0).Write(first+n);return m;}
 static void MatrixIs(GameMovementMatrix48 m,float[] values,string message){for(int n=0;n<12;n++)Equal(values[n],m.At(n,0).Read(0),message+"["+n+"]");}
 static void Sentinel(GameMovementMatrix48 m,int first,string message){for(int n=first;n<12;n++)Equal(99,m.At(n,0).Read(0),message+"["+n+"]");}
 static void Run(){
  Test("Own matrix aliases preserve cells, unknown values and lifetime",delegate{
   var m=new GameMovementMatrix48();var alias=new GameMovementMatrix48(m);Same(m.AddressIdentity,alias.AddressIdentity,"same address");Same(m.At(7,0),alias.At(7,0),"same cell");
   Missing(123,delegate{alias.At(7,123).Read(123);},"new cells remain unknown");var exported=m.Cells;exported[0]=new Field<float>();Check(!Object.ReferenceEquals(exported[0],m.At(0,0)),"array replacement cannot change slot");
   m.At(7,0).Write(12);Equal(12,alias.At(7,0).Read(0),"live shared value");m.MarkReleased();Missing(456,delegate{alias.At(7,456);},"shared lifetime");
  });
  Test("Basis produces translated orthonormal axes and exact output identity",delegate{
   var o=Matrix(99);var i=new Invocation();var got=GameMovementMathContracts.Basis591B80(o,Point(10,20,30),Point(10,20,35),Point(0,1,0),Constants(),i);
   Same(o,got,"return is caller output");Check(i.At==0x591dda,"identity returned after final store");MatrixIs(o,new float[]{1,0,0,0,1,0,0,0,1,10,20,30},"translated identity");
  });
  Test("Non-axis basis preserves handedness and separate reciprocal normalizations",delegate{
   var o=new GameMovementMatrix48();GameMovementMathContracts.Basis591B80(o,Point(2,3,4),Point(5,3,8),Point(0,1,0),Constants(),new Invocation());
   MatrixIs(o,new float[]{0.8f,0,-0.6f,BitConverter.ToSingle(BitConverter.GetBytes(0x80000000u),0),1,0,0.6f,0,0.8f,2,3,4},"3-4-5 basis");
  });
  Test("Difference unknowns stop in X Z Y arithmetic order before first store",delegate{
   foreach(int axis in new[]{0,2,1}){
    var target=Point(3,0,4);var c=axis==0?target.X:axis==1?target.Y:target.Z;c.Forget();var o=Matrix(99);
    uint at=axis==0?0x591b89u:axis==2?0x591b93u:0x591b9eu;
    Missing(at,delegate{GameMovementMathContracts.Basis591B80(o,Point(0,0,0),target,Point(0,1,0),Constants(),new Invocation());},"unknown difference");Sentinel(o,0,"no speculative writes");
   }
  });
  Test("Up unknowns preserve the produced forward row without inventing a basis",delegate{
   foreach(int axis in new[]{2,1,0}){
    var up=Point(0,1,0);(axis==0?up.X:axis==1?up.Y:up.Z).Forget();var o=Matrix(99);
    uint at=axis==2?0x591bd9u:axis==1?0x591be6u:0x591c15u;
    Missing(at,delegate{GameMovementMathContracts.Basis591B80(o,Point(0,0,0),Point(3,0,4),up,Constants(),new Invocation());},"unknown up consumed by multiply");
    for(int n=0;n<6;n++)Equal(99,o.At(n,0).Read(0),"right/up not yet stored");Equal(3,o.At(6,0).Read(0),"forward X already stored");Equal(0,o.At(7,0).Read(0),"forward Y");Equal(4,o.At(8,0).Read(0),"forward Z");Sentinel(o,9,"translation untouched");
   }
  });
  Test("Unknown captured one stops after unnormalized basis stores",delegate{
   var o=Matrix(99);Missing(0x591cce,delegate{GameMovementMathContracts.Basis591B80(o,Point(0,0,0),Point(3,0,4),Point(0,1,0),new GameMovementMathConstants(),new Invocation());},"disk constant must be explicit");
   MatrixIs(o,new float[]{4,0,-3,BitConverter.ToSingle(BitConverter.GetBytes(0x80000000u),0),25,0,3,0,4,99,99,99},"failure prefix retains native intermediate rows");
  });
  Test("Degenerate inputs propagate NaNs rather than fallback axes",delegate{
   var o=new GameMovementMatrix48();GameMovementMathContracts.Basis591B80(o,Point(2,3,4),Point(2,3,4),Point(0,1,0),Constants(),new Invocation());
   for(int n=0;n<9;n++)Check(Single.IsNaN(o.At(n,0).Read(0)),"zero-length normalization NaN");Equal(2,o.At(9,0).Read(0),"translation still produced");Equal(3,o.At(10,0).Read(0),"translation Y");Equal(4,o.At(11,0).Read(0),"translation Z");
   o=new GameMovementMatrix48();GameMovementMathContracts.Basis591B80(o,Point(0,0,0),Point(0,1,0),Point(0,1,0),Constants(),new Invocation());
   for(int n=0;n<6;n++)Check(Single.IsNaN(o.At(n,0).Read(0)),"parallel up has no invented side axis");Equal(1,o.At(7,0).Read(0),"forward normalization still completes");
  });
  Test("Translation MOV copies signed zero unchanged",delegate{
   float negativeZero=BitConverter.ToSingle(BitConverter.GetBytes(0x80000000u),0);var o=new GameMovementMatrix48();GameMovementMathContracts.Basis591B80(o,Point(negativeZero,0,0),Point(0,0,2),Point(0,1,0),Constants(),new Invocation());
   Check(Bits(o.At(9,0).Read(0))==0x80000000u,"origin bits copied after normalization");
  });
  Test("Every output/input shared cell is rejected before mutation",delegate{
   var o=Matrix(99);var p=new NavigationPoint3(o.At(11,0),Field<float>.Of(0),Field<float>.Of(0));Missing(0x591b80,delegate{GameMovementMathContracts.Basis591B80(o,p,Point(0,0,1),Point(0,1,0),Constants(),new Invocation());},"partial output/point overlap");Sentinel(o,0,"basis alias no writes");
   var a=Sequence(1);var b=Sequence(13);var alias=new GameMovementMatrix48(a);Missing(0x07e6f0,delegate{GameMovementMathContracts.Compose07E6F0(alias,a,b,new Invocation());},"same address matrix alias");Equal(1,a.At(0,0).Read(0),"input unchanged");
   var shared=Matrix(99).Cells;shared[11]=a.At(4,0);o=new GameMovementMatrix48(shared);Missing(0x07e6f0,delegate{GameMovementMathContracts.Compose07E6F0(o,a,b,new Invocation());},"nonmatching container partial overlap");Equal(99,o.At(0,0).Read(0),"partial alias rejected before first write");
  });
  Test("Composition matches independent integer product and translation golden",delegate{
   var a=Sequence(1);var b=Sequence(13);var o=new GameMovementMatrix48();var i=new Invocation();Same(o,GameMovementMathContracts.Compose07E6F0(o,a,b,i),"caller output returned");Check(i.At==0x07e922,"normal return location");
   MatrixIs(o,new float[]{174,216,258,210,261,312,246,306,366,292,362,432},"noncommuting integer golden");Equal(1,a.At(0,0).Read(0),"left remains original");Equal(24,b.At(11,0).Read(0),"right remains original");
  });
  Test("Composition preserves exceptional D7 D10 sum order and no fused multiply-add",delegate{
   var a=Matrix(0);var b=Matrix(0);a.At(1,0).Write(1e20f);a.At(4,0).Write(-1e20f);a.At(7,0).Write(1);b.At(6,0).Write(1);b.At(7,0).Write(1);b.At(8,0).Write(1);b.At(9,0).Write(1);b.At(10,0).Write(1);b.At(11,0).Write(1);
   var o=new GameMovementMatrix48();GameMovementMathContracts.Compose07E6F0(o,a,b,new Invocation());Equal(1,o.At(7,0).Read(0),"r+p cancellation precedes q in D7");Equal(1,o.At(10,0).Read(0),"same different order in D10");
   a=Matrix(0);b=Matrix(0);a.At(0,0).Write(1.00000011920928955078125f);b.At(0,0).Write(0.99999988079071044921875f);a.At(3,0).Write(1);b.At(1,0).Write(-1);
   GameMovementMathContracts.Compose07E6F0(o,a,b,new Invocation());Equal(0,o.At(0,0).Read(0),"product rounded before addition, not FMA");
  });
  Test("Composition stops at actual unknown arithmetic and retains scalar prefix",delegate{
   int[] missingCell={1,4,9,10,11};uint[] at={0x07e6ff,0x07e77b,0x07e8b2,0x07e8e5,0x07e918};int[] prefix={0,3,9,10,11};float[] gold={174,216,258,210,261,312,246,306,366,292,362,432};
   for(int test=0;test<missingCell.Length;test++){
    var a=Sequence(1);var b=Sequence(13);var o=Matrix(99);if(test<2)b.At(missingCell[test],0).Forget();else a.At(missingCell[test],0).Forget();uint expected=at[test];
    Missing(expected,delegate{GameMovementMathContracts.Compose07E6F0(o,a,b,new Invocation());},"unknown component");for(int n=0;n<prefix[test];n++)Equal(gold[n],o.At(n,0).Read(0),"retained completed scalar");Sentinel(o,prefix[test],"remaining destination untouched");
   }
  });
  Test("Released local buffers and unresolved trigonometry cannot report success",delegate{
   var a=Sequence(1);var b=Sequence(13);var o=Matrix(99);a.MarkReleased();Missing(0x07e6f0,delegate{GameMovementMathContracts.Compose07E6F0(o,a,b,new Invocation());},"released input");Sentinel(o,0,"no writes on rejected own lifetime");
   Missing(0x591a30,delegate{GameMovementMathContracts.AxisRotation591A10(o,Point(0,1,0),0,new Invocation());},"zero angle does not skip unclosed native cosf");Sentinel(o,0,"no fabricated rotation");
  });
 }
 public static int Main(){try{Run();Console.WriteLine("{\"status\":\"passed\",\"scenarios\":"+scenarios+",\"assertions\":"+assertions+",\"native_execution\":false,\"old_suites_rerun\":false,\"movement_publication\":false,\"trigonometry_closed\":false,\"gameplay_accepted\":false}");return 0;}catch(Exception e){Console.Error.WriteLine(e);return 1;}}
}
