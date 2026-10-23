using System;
using GrimDynasty.ReverseClient.Actions;

public static class GroundRayBoundsTestProgram {
 static int assertions,scenarios;
 static void Check(bool value,string reason){assertions++;if(!value)throw new Exception(reason);}
 static void Case(Action action){action();scenarios++;}
 static void Missing(Action action,uint expected){assertions++;try{action();}catch(MissingContract e){if(e.Address!=expected)throw new Exception("Wrong stop "+e.Address.ToString("x"));return;}throw new Exception("Expected explicit Unsupported");}
 static WorldBounds24 Box(){var b=new WorldBounds24();b.Center00.Write(0);b.Center04.Write(0);b.Center08.Write(0);b.Extent0C.Write(1);b.Extent10.Write(1);b.Extent14.Write(1);return b;}
 static GroundRay24 Ray(float x,float y,float z,float dx,float dy,float dz){var r=new GroundRay24();r.Origin00.X0.Write(x);r.Origin00.Y4.Write(y);r.Origin00.Z8.Write(z);r.Direction0C.X0.Write(dx);r.Direction0C.Y4.Write(dy);r.Direction0C.Z8.Write(dz);return r;}
 static GroundTriangleOperands Operands(){var o=new GroundTriangleOperands();o.SelectObservedDiskOperands();return o;}
 static float Query(WorldBounds24 b,GroundRay24 r){return GroundRayBoundsContracts.Intersection03D780(b,r,Operands(),new Invocation());}
 public static int Main(string[] args){try{
  if(args.Length!=0)throw new Exception("No test arguments");
  Case(delegate{
   var r=Ray(0,0,0,0,0,0);r.Direction0C.X0.Forget();r.Direction0C.Y4.Forget();r.Direction0C.Z8.Forget();
   Check(Query(Box(),r)==0,"Inside origin returns zero without consuming direction");
   r.Origin00.X0.Write(1);r.Origin00.Y4.Write(-1);r.Origin00.Z8.Write(1);Check(Query(Box(),r)==0,"Closed cube corners are inside");
  });
  Case(delegate{
   Check(Query(Box(),Ray(-3,0,0,1,0,0))==2,"Positive X candidate");Check(Query(Box(),Ray(3,0,0,-1,0,0))==2,"Negative X candidate");
   Check(Query(Box(),Ray(0,-3,0,0,1,0))==2,"Positive Y candidate");Check(Query(Box(),Ray(0,3,0,0,-1,0))==2,"Negative Y candidate");
   Check(Query(Box(),Ray(0,0,-3,0,0,1))==2,"Positive Z candidate");Check(Query(Box(),Ray(0,0,3,0,0,-1))==2,"Negative Z candidate");
  });
  Case(delegate{
   Check(float.IsPositiveInfinity(Query(Box(),Ray(-3,0,0,-1,0,0))),"Ray directed away");
   Check(float.IsPositiveInfinity(Query(Box(),Ray(-3,0,0,0,0,0))),"Parallel outside candidate retains minus one");
   Check(float.IsPositiveInfinity(Query(Box(),Ray(-3,0,0,-0.0f,1,0))),"Signed zero is parallel");
   Check(float.IsPositiveInfinity(Query(Box(),Ray(-3,0,0,1,2,0))),"X plane point lies outside Y bounds");
   Check(float.IsPositiveInfinity(Query(Box(),Ray(-3,0,0,1,-2,0))),"Lower Y face miss");
  });
  Case(delegate{
   Check(Query(Box(),Ray(-3,-3,-3,1,1,1))==2,"Three equal candidates retain first axis and hit corner");
   Check(Query(Box(),Ray(-3,3,0,1,-1,0))==2,"Two equal candidates hit edge");
   Check(Query(Box(),Ray(-3,0,0,1,.5f,0))==2,"Inclusive upper face boundary");
   Check(Query(Box(),Ray(-3,0,0,1,-.5f,0))==2,"Inclusive lower face boundary");
   Check(Query(Box(),Ray(-3,-5,0,1,2,0))==2,"Y candidate ties X at entry");
  });
  Case(delegate{
   var b=Box();b.Center00.Write(8);b.Center04.Write(4);b.Center08.Write(-3);b.Extent0C.Write(2);b.Extent10.Write(.5f);b.Extent14.Write(3);
   Check(Query(b,Ray(8,9,-3,0,-2,0))==2.25f,"Translated non-cubic bounds and non-unit direction");
   b.Extent10.Write(0);Check(Query(b,Ray(8,9,-3,0,-2,0))==2.5f,"Zero-height ground bound remains hittable");
  });
  Case(delegate{
   var b=Box();var r=Ray(0,3,0,0,-1,0);b.Center04.Forget();Missing(delegate{Query(b,r);},0x03d789);
   b=Box();b.Extent10.Forget();Missing(delegate{Query(b,r);},0x03d7a5);
   b=Box();r.Direction0C.Y4.Forget();Missing(delegate{Query(b,r);},0x03d9b2);
   r=Ray(0,3,0,0,-1,0);r.Direction0C.X0.Forget();Missing(delegate{Query(b,r);},0x03db14);
   r=Ray(0,3,0,0,-1,0);r.Origin00.Z8.Forget();Missing(delegate{Query(b,r);},0x03d809);
  });
  Case(delegate{
   var o=new GroundTriangleOperands();var i=new Invocation();
   Check(GroundRayBoundsContracts.Intersection03D780(Box(),Ray(0,0,0,0,0,0),o,i)==0,"Unused infinity operand not read on inside path");
   Check(GroundRayBoundsContracts.Intersection03D780(Box(),Ray(0,3,0,0,-1,0),o,i)==2,"Unused infinity operand not read on hit");
   Missing(delegate{GroundRayBoundsContracts.Intersection03D780(Box(),Ray(0,3,0,0,1,0),o,i);},0x03dac2);
   Check(i.At==0x03dac2,"Invocation records missing miss operand");
  });
  Case(delegate{
   var b=Box();var r=Ray(0,3,0,0,-1,0);r.Origin00.X0.Write(float.NaN);Missing(delegate{Query(b,r);},0x03d819);
   r=Ray(0,3,0,0,-1,0);r.Direction0C.Y4.Write(float.PositiveInfinity);Missing(delegate{Query(b,r);},0x03d9b2);
   b.Center00.Write(float.MaxValue);b.Extent0C.Write(float.MaxValue);Missing(delegate{Query(b,Ray(0,3,0,0,-1,0));},0x03d7af);
   Check(r.Origin00.Y4.Read(0)==3&&r.Direction0C.Y4.Read(0)==float.PositiveInfinity,"Read-only query preserves input cells after failure");
  });
  Console.WriteLine("{\"status\":\"passed\",\"scenarios\":"+scenarios+",\"assertions\":"+assertions+",\"original_executed\":false,\"full_floor_collision\":false,\"gameplay_accepted\":false}");return 0;
 }catch(Exception e){Console.Error.WriteLine(e);return 1;}}
}
