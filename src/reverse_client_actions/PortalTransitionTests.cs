using System;
using System.Collections.Generic;
using GrimDynasty.ReverseClient.Actions;

// Explicit Mock imports/storage only: these tests establish ordering of the
// recovered component, not native portal loading, camera operation or movement.
public static class PortalTransitionTests {
 static int scenarios,assertions;
 static void Check(bool value,string message){assertions++;if(!value)throw new Exception(message);}
 static void Equal<T>(T expected,T actual,string message){Check(EqualityComparer<T>.Default.Equals(expected,actual),message+" expected="+expected+" actual="+actual);}
 static void Same(object expected,object actual,string message){Check(Object.ReferenceEquals(expected,actual),message);}
 static void Missing(Outcome r,uint address){Check(!r.ReturnedNormally,"Unexpected success");Equal(address,r.Address,"Failure address");}
 static void Pass(Outcome r){Check(r.ReturnedNormally,"Unexpected stop "+r.Address.ToString("x")+" "+r.Boundary);}
 static void Unknown<T>(Field<T> field){try{field.Read(1);throw new Exception("Unobserved field became known");}catch(MissingContract){assertions++;}}
 static void Test(string name,Action body){body();scenarios++;Console.WriteLine("PASS "+name);}
 static int Count(Mock b,string name){int n=0;foreach(string s in b.Calls)if(s==name)n++;return n;}
 static WaypointMap KnownNullMap(ulong size){var m=new WaypointMap(size);for(ulong n=0;n<size;n++)m.Cell(n,1).Write(null);return m;}
 static void Position(WorldPosition p,ulong x){p.Q0.Write(x);p.Q8.Write(x+1);p.Q16.Write(x+2);}
 static void Matrix(Waypoint80 r,float[] values){for(int k=0;k<9;k++)r.Matrix20[k].Write(values[k]);}
 static float[] Identity(){return new float[]{1,0,0,0,1,0,0,0,1};}
 static float[] Scale(){return new float[]{2,0,0,0,3,0,0,0,4};}
 static void Frame(CoordinateFrame60 f,float[] values){Position(f.Position00,30);for(int k=0;k<9;k++)f.Basis18[k].Write(values[k]);}
 static Waypoint80 Record(uint kind,ulong position,float[] matrix){var r=new Waypoint80();r.Kind00.Write(kind);Position(r.Position08,position);Matrix(r,matrix);return r;}
 static Outcome AttemptNext(F f){return MoveContracts.Run(delegate(Invocation i){WaypointContracts.Next(f.View,f.B,i);});}
 static byte Next(F f){return WaypointContracts.Next(f.View,f.B,new Invocation());}
 static byte Produce(F f){return PortalTransitionContracts.Produce(f.View,f.Destination,f.Portal,f.PriorHidden,f.B,new Invocation());}
 sealed class F {
  public readonly MoveContext MoveContext=new MoveContext();
  public readonly PortalContext PortalContext=new PortalContext();
  public readonly MovementManager Manager=new MovementManager();
  public readonly WaypointManagerView View;
  public readonly WaypointQueue Queue;
  public readonly object Owner=new object(),Portal=new object(),ConnectedPortal=new object(),World=new object();
  public readonly ClassRecord PlayerClass=new ClassRecord(),NonPlayerClass=new ClassRecord();
  public readonly WorldPosition Destination=new WorldPosition(),PriorHidden=new WorldPosition();
  public readonly Mock B;
  public F(){
   View=new WaypointManagerView(Manager);Queue=View.Queue40;var proxy=new WaypointProxy();proxy.Container00.Write(Queue);Queue.Proxy00.Write(proxy);
   Queue.Map08.Write(KnownNullMap(8));Queue.MapSize10.Write(8);Queue.Offset18.Write(0);Queue.Count20.Write(0);
   Manager.Owner00.Write(Owner);Manager.Moving38.Write(7);Manager.Registered6C.Write(0);Position(Destination,80);Position(PriorHidden,30);
   var engine=new WorldEngine();engine.World50.Write(World);MoveContext.EngineGlobal.Write(engine);
   PortalContext.PlayerClassA5CAB0.Write(PlayerClass);PortalContext.Pi7A80BC.Write((float)Math.PI);NonPlayerClass.Parent10.Write(null);
   B=new Mock(MoveContext,PortalContext);B.Attach(View);B.ConnectedValue=ConnectedPortal;B.PortalValue=Portal;B.Classes[Owner]=NonPlayerClass;
  }
  public Waypoint80 Seed(float[] matrix){var r=Record(1,80,matrix);Queue.Map08.Read(1).Cell(0,1).Write(r);Queue.Count20.Write(1);return r;}
 }
 sealed class Mock:PortalTransitionBoundaries {
  public readonly List<string> Calls=new List<string>();
  public readonly List<Waypoint80> Allocations=new List<Waypoint80>();
  public readonly Dictionary<object,ClassRecord> Classes=new Dictionary<object,ClassRecord>();
  public readonly Dictionary<uint,object> Objects=new Dictionary<uint,object>();
  public readonly List<PlayerConnection> Connections=new List<PlayerConnection>();
  public Action<string,Mock> On=null;
  public object Arg0=null,Arg1=null,Arg2=null,ConnectedValue=null,PortalValue=null;
  public uint U0=0;public float F0=0,F1=0;
  public readonly object NavigationValue=new object(),RegionValue=new object(),ObjectManagerValue=new object();
  public readonly object SelectionSourceRegion=new object(),SelectionDestinationRegion=new object();
  public readonly WorldPosition FirstChokeReturn=new WorldPosition(),SecondChokeReturn=new WorldPosition(),CoordsReturn=new WorldPosition();
  public readonly CoordinateFrame60 IgnoredFrameReturn=new CoordinateFrame60();
  public CoordinateFrame60 OwnerHidden=null,CameraHidden=null;
  public WorldPosition LastOwnerCoordsHidden=null;
  public bool EnableSelection=false,PartialOriginalChoke=false;
  public float[] OwnerBasis={1,2,3,4,5,6,7,8,9};
  public float[] CameraBasis={1,0,0,0,1,0,2,3,4};
  public float[] MatrixValues=Identity();
  public float AtanValue=7;
  public byte DerivedValue=0,NavigationAccepted=0;
  public Mock(MoveContext m,PortalContext p):base(m,p){Position(FirstChokeReturn,999);Position(SecondChokeReturn,20);Position(CoordsReturn,30);}
  void Hit(string name){Calls.Add(name);if(On!=null)On(name,this);}
  public override Waypoint80 Allocate80(){Hit("allocate80");var r=new Waypoint80();Allocations.Add(r);return r;}
  public override WaypointMap AllocateMap0162D0(ulong slots,uint stride){Hit("allocate-map");return new WaypointMap(slots);}
  public override void ReleaseMap016620(WaypointMap map,ulong slots,uint stride){Hit("release-map");map.Released=true;}
  public override void FreeBlock(Waypoint80 block){Arg0=block;Hit("free-block");}
  public override void FreeMap(WaypointMap map,ulong slots){Arg0=map;Hit("free-map");map.Released=true;}
  public override WorldPosition Choke609F30(object portal,WorldPosition output){Arg0=portal;Arg1=output;Hit("choke");bool connected=Object.ReferenceEquals(portal,ConnectedValue);if(!connected&&PartialOriginalChoke)output.Q0.Write(10);else Position(output,connected?777UL:10UL);return connected?SecondChokeReturn:FirstChokeReturn;}
  public override object ConnectedPortal609F40(object portal){Arg0=portal;Hit("connected");return ConnectedValue;}
  public override void PortalMatrix609F48(object portal,Field<float>[] matrix,Field<ulong> scratch){Arg0=portal;Arg1=matrix;Arg2=scratch;Hit("matrix");for(int k=0;k<9;k++)matrix[k].Write(MatrixValues[k]);scratch.Write(123456);}
  public override void InitializePosition609720(WorldPosition p){Arg0=p;Hit("initialize");p.Q0.Write((ulong)(1000+Count(this,"initialize")));}
  public override Displacement3 WorldVector609D98(WorldPosition position,Displacement3 output){Arg0=position;Arg1=output;Hit("vector");var r=new Displacement3();r.X0.Write(1);r.Y4.Write(2);r.Z8.Write(3);return r;}
  public override object Navigation(){Hit("navigation");return NavigationValue;}
  public override byte MoveObject609F08(object navigation,object owner,Displacement3 vector){Arg0=navigation;Arg1=owner;Arg2=vector;Hit("move-object");return NavigationAccepted;}
  public override WorldPosition OwnerCoords(object owner,WorldPosition output){LastOwnerCoordsHidden=output;Position(output,30);Arg0=owner;Arg1=output;Hit("owner-coords");return CoordsReturn;}
  public override object Region(WorldPosition p){Arg0=p;Hit("region");return EnableSelection?(Count(this,"region")==1?SelectionDestinationRegion:SelectionSourceRegion):RegionValue;}
  public override int PortalCount609F10(object region){Arg0=region;Hit("portal-count");return 1;}
  public override object PortalAt609F18(object region,int index){Arg0=region;Hit("portal-at");return PortalValue;}
  public override byte PortalOpen609F20(object portal){Arg0=portal;Hit("portal-open");return 1;}
  public override object PortalRegion609F28(object portal){Arg0=portal;Hit("portal-region");return SelectionDestinationRegion;}
  public override Displacement3 DifferenceReturning609A48(WorldPosition to,Displacement3 output,WorldPosition from){Arg0=to;Arg1=output;Arg2=from;Hit("difference");var result=new Displacement3();result.X0.Write(Count(this,"difference")==1?1:2);result.Y4.Write(0);result.Z8.Write(0);return result;}
  public override void PutOnFloor609BB0(WorldPosition p){Arg0=p;Hit("floor");}
  public override CoordinateFrame60 OwnerFrame609518(object owner,CoordinateFrame60 hidden){OwnerHidden=hidden;Frame(hidden,OwnerBasis);Arg0=owner;Arg1=hidden;Hit("owner-frame");return IgnoredFrameReturn;}
  public override CoordinateFrame60 CameraFrame609F00(PortalCamera camera,CoordinateFrame60 hidden){CameraHidden=hidden;Frame(hidden,CameraBasis);Arg0=camera;Arg1=hidden;Hit("camera-frame");return IgnoredFrameReturn;}
  public override ClassRecord Class0(object value){Arg0=value;Hit("class");return Classes[value];}
  public override byte Derived(ClassRecord parent,ClassRecord target){Arg0=parent;Arg1=target;Hit("derived");return DerivedValue;}
  public override PlayerConnection Connection18(object manager){Arg0=manager;Hit("connection");return Connections[Count(this,"connection")-1];}
  public override object ObjectManager(){Hit("object-manager");return ObjectManagerValue;}
  public override object Lookup(object manager,uint id){Arg0=manager;U0=id;Hit("lookup");object value;return Objects.TryGetValue(id,out value)?value:null;}
  public override float Atan2F5DC3F8(float y,float x){F0=y;F1=x;Hit("atan");return AtanValue;}
  public override void SetFrame609BB8(object world,object owner,CoordinateFrame60 frame){Arg0=world;Arg1=owner;Arg2=frame;Hit("set-frame");}
  public override void Notify609F68(object navigation,object owner){Arg0=navigation;Arg1=owner;Hit("notify");}
 }
 static PortalGameEngine ConfigurePlayer(F f){
  f.B.Classes[f.Owner]=f.PlayerClass;f.B.Objects[7]=f.Owner;
  var first=new PlayerConnection();first.PlayerId10.Write(7);var second=new PlayerConnection();second.PlayerId10.Write(7);f.B.Connections.Add(first);f.B.Connections.Add(second);
  var engine=new PortalGameEngine();engine.Connections40F8.Write(new object());var table=new PortalCameraVTable();table.Slot10.Write(delegate(PortalCamera c,float angle){});engine.Camera13B0.Virtual00.Write(table);f.PortalContext.GameGlobalA66288.Write(engine);return engine;
 }

 static void RunAll(){
  Test("portal producer records zero one zero and honors both choke pointer conventions",delegate{
   var f=new F();var initialized=new List<object>();Field<ulong> scratch=null;
   f.B.On=delegate(string n,Mock p){
    if(n=="choke")Same(Count(p,n)==1?f.Portal:f.ConnectedPortal,p.Arg0,"Choke source order");
    if(n=="initialize")initialized.Add(p.Arg0);
    if(n=="matrix"){Same(f.Portal,p.Arg0,"Matrix original portal");scratch=(Field<ulong>)p.Arg2;Equal(0UL,scratch.Read(1),"Independent scratch starts with explicit zero");}
   };
   Equal((byte)0,Produce(f),"First observed navigation rejection");Equal(3,initialized.Count,"Three constructors");Check(!Object.ReferenceEquals(initialized[0],initialized[1])&&!Object.ReferenceEquals(initialized[1],initialized[2]),"Independent inline positions");Equal(3,f.B.Allocations.Count,"Three waypoint allocations");
   var a=f.B.Allocations[0];var b=f.B.Allocations[1];var c=f.B.Allocations[2];Equal(0u,a.Kind00.Read(1),"Approach kind0");Equal(1u,b.Kind00.Read(1),"Transition kind1");Equal(0u,c.Kind00.Read(1),"Finish kind0");Equal(10UL,a.Position08.Q0.Read(1),"First choke uses hidden output not999 return");Equal(20UL,b.Position08.Q0.Read(1),"Second choke uses returned pointer not777 hidden");Equal(80UL,c.Position08.Q0.Read(1),"Destination finish");Unknown(b.Auxiliary48);Unknown(b.Padding44);Equal(123456UL,scratch.Read(1),"Scratch mutation stays separate");Equal(2UL,f.Queue.Count20.Read(1),"Approach consumed by Next");Equal(1UL,f.Queue.Offset18.Read(1),"Transition remains at front");
  });
  Test("second append failure retains first queued waypoint and initialized prefix",delegate{
   var f=new F();f.B.On=delegate(string n,Mock p){if(n=="allocate80"&&Count(p,n)==2)throw new Exception("fixture second allocation");};
   Missing(MoveContracts.Run(delegate(Invocation i){PortalTransitionContracts.Produce(f.View,f.Destination,f.Portal,f.PriorHidden,f.B,i);}),0x08f51c);
   Equal(1UL,f.Queue.Count20.Read(1),"First append remains");Equal(1,f.B.Allocations.Count,"Only first allocation returned");Same(f.B.Allocations[0],f.Queue.Map08.Read(1).Cell(0,1).Read(1),"First record retained");Equal(3,Count(f.B,"initialize"),"All constructors already ran");Equal(1,Count(f.B,"matrix"),"Matrix prefix completed");Equal(0,Count(f.B,"move-object"),"No Next before all appends");
  });
  Test("Move passes original hidden coords into partial first producer Choke",delegate{
   var f=new F();f.B.EnableSelection=true;f.B.PartialOriginalChoke=true;Position(f.B.CoordsReturn,600);WorldPosition producerHidden=null,connectedHidden=null;
   f.B.On=delegate(string n,Mock p){if(n=="choke"){
    if(Count(p,n)==2){producerHidden=(WorldPosition)p.Arg1;Same(p.LastOwnerCoordsHidden,producerHidden,"OwnerCoords and first producer Choke share stack identity");Equal(31UL,producerHidden.Q8.Read(1),"Prior hidden word before partial Choke");}
    if(Object.ReferenceEquals(p.Arg0,f.ConnectedPortal))connectedHidden=(WorldPosition)p.Arg1;
   }};
   byte result=0;Pass(MoveContracts.Run(delegate(Invocation i){result=f.B.MoveManager(f.Manager,f.Destination);}));Equal((byte)0,result,"Approach navigation fixture rejects");Equal(3,Count(f.B,"choke"),"Selection plus two producer calls");
   var approach=f.B.Allocations[0];Equal(10UL,approach.Position08.Q0.Read(1),"Partial Choke first word");Equal(31UL,approach.Position08.Q8.Read(1),"Prior hidden tail retained, not returned coords601");Equal(32UL,approach.Position08.Q16.Read(1),"Prior last word retained");Check(!Object.ReferenceEquals(producerHidden,connectedHidden),"Connected Choke uses distinct hidden buffer");Equal(20UL,f.B.Allocations[1].Position08.Q0.Read(1),"Connected returned pointer still used");
  });
  Test("ResolvePlayer null exact and direct-parent cases preserve object identity",delegate{
   var f=new F();Same(null,PortalTransitionContracts.ResolvePlayer(null,f.B,new Invocation()),"Null stays null");Equal(0,Count(f.B,"class"),"Null requires no virtual call");
   f.B.Classes[f.Owner]=f.PlayerClass;Same(f.Owner,PortalTransitionContracts.ResolvePlayer(f.Owner,f.B,new Invocation()),"Exact class retains original object");var child=new ClassRecord();child.Parent10.Write(f.PlayerClass);f.B.Classes[f.Owner]=child;Same(f.Owner,PortalTransitionContracts.ResolvePlayer(f.Owner,f.B,new Invocation()),"Direct parent retains original object");Equal(0,Count(f.B,"derived"),"No unnecessary walk");
  });
  Test("ResolvePlayer deeper class walk and unknown parent remain distinct",delegate{
   var f=new F();var parent=new ClassRecord();var child=new ClassRecord();child.Parent10.Write(parent);f.B.Classes[f.Owner]=child;f.B.DerivedValue=255;
   f.B.On=delegate(string n,Mock p){if(n=="derived"){Same(parent,p.Arg0,"Walk starts at parent");Same(f.PlayerClass,p.Arg1,"Fixed target identity");}};
   Same(f.Owner,PortalTransitionContracts.ResolvePlayer(f.Owner,f.B,new Invocation()),"Nonzero AL accepts");f.B.DerivedValue=0;Same(null,PortalTransitionContracts.ResolvePlayer(f.Owner,f.B,new Invocation()),"False walk rejects");child.Parent10.Forget();Missing(MoveContracts.Run(delegate(Invocation i){PortalTransitionContracts.ResolvePlayer(f.Owner,f.B,i);}),0x01924f);
  });
  Test("MainPlayer repeats connection call reads changed manager and captures late ID",delegate{
   var f=new F();var engine=new PortalGameEngine();object firstManager=new object(),secondManager=new object();engine.Connections40F8.Write(firstManager);var first=new PlayerConnection();first.PlayerId10.Write(1);var second=new PlayerConnection();second.PlayerId10.Write(2);f.B.Connections.Add(first);f.B.Connections.Add(second);f.B.Objects[77]=f.Owner;f.B.Classes[f.Owner]=f.PlayerClass;
   f.B.On=delegate(string n,Mock p){if(n=="connection"){if(Count(p,n)==1){Same(firstManager,p.Arg0,"Initial manager");engine.Connections40F8.Write(secondManager);}else{Same(secondManager,p.Arg0,"Manager reread");second.PlayerId10.Write(77);}}if(n=="object-manager")second.PlayerId10.Write(999);if(n=="lookup")Equal(77u,p.U0,"ID captured after second connection before singleton");};
   Same(f.Owner,PortalTransitionContracts.MainPlayer(engine,f.B,new Invocation()),"Same looked-up player");Equal(2,Count(f.B,"connection"),"Two connection virtuals");
  });
  Test("MainPlayer first zero ID exits but second zero still reaches lookup",delegate{
   var f=new F();var engine=new PortalGameEngine();engine.Connections40F8.Write(new object());var zero=new PlayerConnection();zero.PlayerId10.Write(0);f.B.Connections.Add(zero);Same(null,PortalTransitionContracts.MainPlayer(engine,f.B,new Invocation()),"First zero means no main player");Equal(1,Count(f.B,"connection"),"Only first connection");Equal(0,Count(f.B,"lookup"),"No lookup");
   var g=new F();var engine2=new PortalGameEngine();engine2.Connections40F8.Write(new object());var nonzero=new PlayerConnection();nonzero.PlayerId10.Write(1);g.B.Connections.Add(nonzero);g.B.Connections.Add(zero);g.B.Objects[0]=g.Owner;g.B.Classes[g.Owner]=g.PlayerClass;Same(g.Owner,PortalTransitionContracts.MainPlayer(engine2,g.B,new Invocation()),"Second zero forwarded to observed lookup");Equal(1,Count(g.B,"lookup"),"Second ID has no duplicate zero gate");
  });
  Test("null class and connection identities stop at their actual dereference points",delegate{
   var f=new F();f.B.Classes[f.Owner]=null;Missing(MoveContracts.Run(delegate(Invocation i){PortalTransitionContracts.ResolvePlayer(f.Owner,f.B,i);}),0x01924f);
   var first=new F();var firstEngine=new PortalGameEngine();firstEngine.Connections40F8.Write(null);Missing(MoveContracts.Run(delegate(Invocation i){PortalTransitionContracts.MainPlayer(firstEngine,first.B,i);}),0x2ebb80);Equal(0,Count(first.B,"connection"),"Null manager fails before virtual call");
   var second=new F();var secondEngine=new PortalGameEngine();secondEngine.Connections40F8.Write(new object());var connection=new PlayerConnection();connection.PlayerId10.Write(1);second.B.Connections.Add(connection);second.B.On=delegate(string n,Mock p){if(n=="connection")secondEngine.Connections40F8.Write(null);};
   Missing(MoveContracts.Run(delegate(Invocation i){PortalTransitionContracts.MainPlayer(secondEngine,second.B,i);}),0x2ebb93);Equal(1,Count(second.B,"connection"),"Second null manager fails before second virtual call");
   var mainClass=new F();var mainEngine=ConfigurePlayer(mainClass);mainClass.B.Classes[mainClass.Owner]=null;Missing(MoveContracts.Run(delegate(Invocation i){PortalTransitionContracts.MainPlayer(mainEngine,mainClass.B,i);}),0x2ebbc8);
  });
  Test("Next kind1 transforms full hidden frame ignores return and bypasses nonplayer camera",delegate{
   var f=new F();f.Seed(Scale());float[] expected={2,6,12,8,15,24,14,24,36};
   f.B.On=delegate(string n,Mock p){if(n=="set-frame"){Same(f.World,p.Arg0,"World identity");Same(f.Owner,p.Arg1,"Owner identity");Same(p.OwnerHidden,p.Arg2,"Full hidden frame forwarded, not return object");var frame=(CoordinateFrame60)p.Arg2;for(int k=0;k<9;k++)Equal(expected[k],frame.Basis18[k].Read(1),"Transformed basis"+k);Equal(80UL,frame.Position00.Q0.Read(1),"Portal position in frame");Equal(82UL,frame.Position00.Q16.Read(1),"Full position tail");}};
   Equal((byte)0,Next(f),"Recursive empty Next false after transition");Equal(0,Count(f.B,"camera-frame"),"Nonplayer bypasses camera");Equal(0,Count(f.B,"connection"),"No main-player lookup");Equal((byte)0,f.Manager.Moving38.Read(1),"Transition reset");Equal(80UL,f.Manager.Position20.Q0.Read(1),"Manager target copied");Unknown(f.PortalContext.GameGlobalA66288);
  });
  Test("frame transform rounds each multiply and ordered addition to binary32",delegate{
   var frame=new CoordinateFrame60();Frame(frame,new float[]{1,1,1,1,1,1,1,1,1});var record=Record(1,50,new float[]{1,0,0,16777216,0,0,1,0,0});object owner=new object();
   Same(owner,PortalFrameMath.TransformOwner(frame,record,Field<object>.Of(owner),new Invocation()),"Captured owner identity");Equal(16777216.0f,frame.Basis18[0].Read(1),"Huge+1+1 rounds at each add, not double16777218");Equal(16777216.0f,frame.Basis18[3].Read(1),"Second vector same operation order");Equal(16777216.0f,frame.Basis18[6].Read(1),"Third vector same order");
  });
  Test("unknown Owner00 stops at08DC51 after exact partial frame writes",delegate{
   var f=new F();f.Seed(Scale());f.B.On=delegate(string n,Mock p){if(n=="owner-frame")f.Manager.Owner00.Forget();};
   Missing(AttemptNext(f),0x08dc51);var frame=f.B.OwnerHidden;Equal(80UL,frame.Position00.Q0.Read(1),"Position already replaced");Equal(2.0f,frame.Basis18[0].Read(1),"First vector x written");Equal(6.0f,frame.Basis18[1].Read(1),"First vector y written");Equal(12.0f,frame.Basis18[2].Read(1),"First vector z written");for(int k=3;k<9;k++)Equal((float)(k+1),frame.Basis18[k].Read(1),"Later vector store not reached"+k);Equal(0,Count(f.B,"class"),"No classification after unknown owner");Equal(0,Count(f.B,"set-frame"),"No world operation");Equal(0UL,f.Queue.Count20.Read(1),"Waypoint consumption retained");
  });
  Test("nonplayer transition resets state then recursively consumes following kind0 rejection",delegate{
   var f=new F();f.Seed(Identity());f.Queue.Map08.Read(1).Cell(1,1).Write(Record(0,90,Identity()));f.Queue.Count20.Write(2);
   Equal((byte)0,Next(f),"Recursive navigation rejection result");Equal(1,Count(f.B,"set-frame"),"Transition committed once");Equal(1,Count(f.B,"move-object"),"Following waypoint attempted");Equal(0UL,f.Queue.Count20.Read(1),"Both records consumed");Equal(0UL,f.Queue.Offset18.Read(1),"Empty offset reset");Equal((byte)0,f.Manager.Moving38.Read(1),"Transition reset retained across next rejection");
  });
  Test("different main player skips camera but still changes world frame",delegate{
   var f=new F();f.Seed(Identity());ConfigurePlayer(f);var other=new object();f.B.Objects[7]=other;f.B.Classes[other]=f.PlayerClass;
   Equal((byte)0,Next(f),"Transition returns recursive empty result");Equal(2,Count(f.B,"connection"),"Main player checked");Equal(0,Count(f.B,"camera-frame"),"Identity mismatch skips camera");Equal(1,Count(f.B,"set-frame"),"World frame still written");
  });
  Test("camera uses three game-global reads and captured table with late slot load",delegate{
   var f=new F();f.Seed(Identity());var first=ConfigurePlayer(f);var second=new PortalGameEngine();var third=new PortalGameEngine();var fourth=new PortalGameEngine();var capturedTable=new PortalCameraVTable();var replacementTable=new PortalCameraVTable();int earlyCalls=0,lateCalls=0,replacementCalls=0;
   capturedTable.Slot10.Write(delegate(PortalCamera camera,float angle){earlyCalls++;});replacementTable.Slot10.Write(delegate(PortalCamera camera,float angle){replacementCalls++;});third.Camera13B0.Virtual00.Write(capturedTable);
   f.B.On=delegate(string n,Mock p){
    if(n=="connection")Same(first.Connections40F8.Read(1),p.Arg0,"First global serves MainPlayer");
    if(n=="lookup")f.PortalContext.GameGlobalA66288.Write(second);
    if(n=="camera-frame"){Same(second.Camera13B0,p.Arg0,"Second global selects frame camera");f.PortalContext.GameGlobalA66288.Write(third);}
    if(n=="atan"){
     Equal(2.0f,p.F0,"atan y from hidden frame, ignored return stayed unknown");Equal(4.0f,p.F1,"atan x from transformed hidden frame");
     third.Camera13B0.Virtual00.Write(replacementTable);capturedTable.Slot10.Write(delegate(PortalCamera camera,float angle){lateCalls++;Same(third.Camera13B0,camera,"Receiver captured from third global");Equal((float)(7.0f-(float)Math.PI),angle,"Rounded angle minus explicit pi");});f.PortalContext.GameGlobalA66288.Write(fourth);
    }
   };
   Equal((byte)0,Next(f),"Transition completed");Equal(0,earlyCalls,"Slot not captured before atan");Equal(1,lateCalls,"Old table late slot used");Equal(0,replacementCalls,"New vtable not reread after atan");Equal(1,Count(f.B,"set-frame"),"World update follows camera");
  });
  Test("unknown pi stops after atan before camera setter and world update",delegate{
   var f=new F();f.Seed(Identity());var engine=ConfigurePlayer(f);int setters=0;engine.Camera13B0.Virtual00.Read(1).Slot10.Write(delegate(PortalCamera c,float angle){setters++;});f.PortalContext.Pi7A80BC.Forget();
   Missing(AttemptNext(f),0x08ddf8);Equal(1,Count(f.B,"atan"),"atan already returned");Equal(0,setters,"No guessed pi");Equal(0,Count(f.B,"set-frame"),"World update not reached");Equal(0UL,f.Queue.Count20.Read(1),"Record consumed");Equal((byte)7,f.Manager.Moving38.Read(1),"No transition success reset");Unknown(f.Manager.Position20.Q0);
  });
  Test("known-null camera receiver faults only after transformed frame stores",delegate{
   var f=new F();f.Seed(Scale());ConfigurePlayer(f);f.B.On=delegate(string n,Mock p){if(n=="camera-frame")f.PortalContext.GameGlobalA66288.Write(null);};
   Missing(AttemptNext(f),0x08ddec);Equal(1,Count(f.B,"camera-frame"),"Frame callback completed before late global became null");Equal(4.0f,f.B.CameraHidden.Basis18[6].Read(1),"Camera x stored after known-null receiver capture");Equal(9.0f,f.B.CameraHidden.Basis18[7].Read(1),"Camera y stored before receiver dereference");Equal(16.0f,f.B.CameraHidden.Basis18[8].Read(1),"Camera z stored before receiver dereference");Equal(0,Count(f.B,"atan"),"Receiver dereference precedes atan");Equal(0,Count(f.B,"set-frame"),"World update not reached");Equal(0UL,f.Queue.Count20.Read(1),"Consumed transition is not restored");Equal((byte)7,f.Manager.Moving38.Read(1),"Success reset not reached");Unknown(f.Manager.Position20.Q0);
  });
  Test("known-null camera vptr permits atan and pi before slot dereference",delegate{
   var f=new F();f.Seed(Scale());var engine=ConfigurePlayer(f);engine.Camera13B0.Virtual00.Write(null);
   Missing(AttemptNext(f),0x08de0a);Equal(1,Count(f.B,"atan"),"Known-null table capture does not skip atan");Equal(4.0f,f.B.CameraHidden.Basis18[6].Read(1),"Camera x prefix retained");Equal(9.0f,f.B.CameraHidden.Basis18[7].Read(1),"Camera y prefix retained");Equal(16.0f,f.B.CameraHidden.Basis18[8].Read(1),"Camera z prefix retained");Equal(0,Count(f.B,"set-frame"),"No world update after null slot receiver");Equal(0UL,f.Queue.Count20.Read(1),"Consumption retained");Equal((byte)7,f.Manager.Moving38.Read(1),"Reset not reached");Unknown(f.Manager.Position20.Q0);
   var g=new F();g.Seed(Identity());var second=ConfigurePlayer(g);second.Camera13B0.Virtual00.Write(null);g.PortalContext.Pi7A80BC.Forget();
   Missing(AttemptNext(g),0x08ddf8);Equal(1,Count(g.B,"atan"),"Unknown pi is observed after atan even with null table");Equal(0,Count(g.B,"set-frame"),"Unknown pi also stops before world update");
  });
  Test("camera setter failure preserves frame and consumed transition before target copy",delegate{
   var f=new F();f.Seed(Scale());var engine=ConfigurePlayer(f);int setters=0;var failure=new InvalidOperationException("fixture camera setter");
   engine.Camera13B0.Virtual00.Read(1).Slot10.Write(delegate(PortalCamera camera,float angle){setters++;Same(engine.Camera13B0,camera,"Setter receives captured camera identity");Equal((float)(7.0f-(float)Math.PI),angle,"Setter observes angle after explicit pi subtraction");throw failure;});
   Outcome result=AttemptNext(f);Missing(result,0x08de0a);Same(failure,result.Exception.InnerException,"Original setter exception retained");Equal(1,Count(f.B,"atan"),"Atan completed before setter");Equal(1,setters,"Setter entered once");Equal(4.0f,f.B.CameraHidden.Basis18[6].Read(1),"Camera x transformation retained");Equal(9.0f,f.B.CameraHidden.Basis18[7].Read(1),"Camera y transformation retained");Equal(16.0f,f.B.CameraHidden.Basis18[8].Read(1),"Camera z transformation retained");Equal(0,Count(f.B,"set-frame"),"World update not reached");Equal(0,Count(f.B,"notify"),"No navigation notification");Equal(0UL,f.Queue.Count20.Read(1),"Transition remains consumed");Equal((byte)7,f.Manager.Moving38.Read(1),"Success reset not reached");Unknown(f.Manager.Position20.Q0);
  });
  Test("atan failure preserves transformed camera hidden frame and consumed queue",delegate{
   var f=new F();f.Seed(Scale());ConfigurePlayer(f);f.B.On=delegate(string n,Mock p){if(n=="atan")throw new InvalidOperationException("fixture atan");};
   Outcome result=AttemptNext(f);Missing(result,0x08ddf3);Check(result.Exception.InnerException is InvalidOperationException,"Exception retained");Equal(4.0f,f.B.CameraHidden.Basis18[6].Read(1),"Camera x already transformed");Equal(9.0f,f.B.CameraHidden.Basis18[7].Read(1),"Camera y already transformed");Equal(16.0f,f.B.CameraHidden.Basis18[8].Read(1),"Camera z already transformed");Equal(0,Count(f.B,"set-frame"),"World update not reached");Equal(0UL,f.Queue.Count20.Read(1),"Consumption retained");
  });
  Test("world frame failure retains target copy without reset or recursive advance",delegate{
   var f=new F();f.Seed(Identity());f.B.On=delegate(string n,Mock p){if(n=="set-frame")throw new Exception("fixture world");};
   Missing(AttemptNext(f),0x08de37);Equal(80UL,f.Manager.Position20.Q0.Read(1),"Target copy precedes world call");Equal(82UL,f.Manager.Position20.Q16.Read(1),"Target tail copied");Equal((byte)7,f.Manager.Moving38.Read(1),"Reset not reached");Equal(0,Count(f.B,"notify"),"No notification after failure");
  });
  Test("registered transition rereads owner after navigation and failure precedes reset",delegate{
   var f=new F();f.Seed(Identity());f.Manager.Registered6C.Write(1);object late=new object();
   f.B.On=delegate(string n,Mock p){if(n=="navigation")f.Manager.Owner00.Write(late);if(n=="notify"){Same(late,p.Arg1,"Owner read after singleton");throw new Exception("fixture notify");}};
   Missing(AttemptNext(f),0x08de57);Equal(1,Count(f.B,"set-frame"),"World prefix completed");Equal((byte)7,f.Manager.Moving38.Read(1),"Moving reset not reached");Equal(0,Count(f.B,"vector"),"No recursive next after failure");
  });
  Test("base default kind1 remains Unsupported after new extension hook",delegate{
   var f=new F();f.Seed(Identity());var originalBoundary=new WaypointBoundaries(f.MoveContext);
   Missing(MoveContracts.Run(delegate(Invocation i){WaypointContracts.Next(f.View,originalBoundary,i);}),0x08dafe);Equal(0UL,f.Queue.Count20.Read(1),"Old consume prefix retained");Equal((byte)7,f.Manager.Moving38.Read(1),"No new default portal behavior");
  });
  Test("no-portal Move still produces ordinary kind0 without portal extension calls",delegate{
   var f=new F();f.Queue.Map08.Write(null);f.Queue.MapSize10.Write(0);byte result=0;
   Pass(MoveContracts.Run(delegate(Invocation i){result=f.B.MoveManager(f.Manager,f.Destination);}));Equal((byte)0,result,"Observed kind0 navigation rejection");Equal(0,Count(f.B,"choke"),"Portal producer not invoked");Equal(0,Count(f.B,"owner-frame"),"Portal executor not invoked");Equal(1,f.B.Allocations.Count,"One ordinary waypoint");Equal(0u,f.B.Allocations[0].Kind00.Read(1),"Ordinary kind0");Equal(80UL,f.B.Allocations[0].Position08.Q0.Read(1),"Destination unchanged");Equal(0UL,f.Queue.Count20.Read(1),"Ordinary Next consumed it");
  });
 }
 public static int Main(){try{RunAll();Console.WriteLine("PASS "+scenarios+" scenarios, "+assertions+" assertions; mock portal/frame observations only, no native gameplay validation");return 0;}catch(Exception x){Console.Error.WriteLine("FAIL after "+scenarios+" scenarios / "+assertions+" assertions: "+x);return 1;}}
}
