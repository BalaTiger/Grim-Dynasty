using System;
using System.Collections.Generic;

namespace GrimDynasty.ReverseClient.Actions {
 public sealed class ControllerCurrentDataInvocation {
  public readonly Invocation Native=new Invocation();
  public ControllerStateData40 Result;
  public bool Returned,OverlaySelected;
  internal void Begin(){Native.At=0;Result=null;Returned=OverlaySelected=false;}
 }
 public sealed class ControllerMoveToEntryInvocation {
  public readonly Invocation Native=new Invocation(),Coordinates=new Invocation(),Difference=new Invocation(),RunSpeed=new Invocation();
  public ControllerCurrentDataInvocation FirstData,SecondData;
  public PlayerMousePlayerLookupInvocation Lookup;
  public WorldPosition24 Destination,Source,FreshDestination;
  public Displacement3 Delta;
  public Field<ulong> RegionToken;
  public bool RegionReturned,CoordinatesReturned,DifferenceReturned,DirectionWritten,PreviousWritten,RunSpeedEntered,Returned;
  public int LookupCalls;
  public ControllerMoveToEntryInvocation(){Begin();}
  internal void Begin(){Native.At=Coordinates.At=Difference.At=RunSpeed.At=0;FirstData=new ControllerCurrentDataInvocation();SecondData=new ControllerCurrentDataInvocation();Lookup=new PlayerMousePlayerLookupInvocation();Destination=new WorldPosition24();Source=new WorldPosition24();FreshDestination=new WorldPosition24();Delta=new Displacement3();RegionToken=new Field<ulong>();RegionReturned=CoordinatesReturned=DifferenceReturned=DirectionWritten=PreviousWritten=RunSpeedEntered=Returned=false;LookupCalls=0;}
 }
 public sealed class ControllerStateTransitionInvocation {
  public readonly Invocation Native=new Invocation();
  public ControllerCurrentDataInvocation CurrentData;
  public ControllerMoveToEntryInvocation Entry;
  public bool LookupReturned,ExitReturned,DataCopied,StatePublished,EntryReturned,Returned;
  public ControllerStateTransitionInvocation(){Begin();}
  internal void Begin(){Native.At=0;CurrentData=new ControllerCurrentDataInvocation();Entry=new ControllerMoveToEntryInvocation();LookupReturned=ExitReturned=DataCopied=StatePublished=EntryReturned=Returned=false;}
 }
 // Actual123D50/125730/125A00/166020 and078960/019E40 bodies. No API or
 // callback can inject a candidate, comparison, speed or successful action.
 public static class ControllerStateTransitionContracts {
  static T Need<T>(T value,uint at)where T:class{if(value==null)throw new MissingContract(at,"Null own controller-state view");return value;}
  static uint At(Invocation i,uint at){i.At=at;return at;}
  static float F(float value){return NavigationCoreContracts.F(value);}
  static ControllerStateTransitionState State(Controller controller,ControllerStateTransitionContext context,uint at){return ControllerStateTransitionState.ForBound(Need(context,at).Lookup.Mouse,controller,at);}
  // Both recovered callers identify5DC3EC as memcmp. This explicit OWN
  // bounded-byte adaptation preserves its zero/sign contract; a nonzero
  // result is the first unsigned-byte difference, not a claim about the exact
  // native CRT magnitude/read granularity. No culture, text decoding, pointer
  // callback or native CRT execution is involved. Heap names are out of scope.
  static int CompareOwnBytes(PlayerMoveShortName first,PlayerMoveShortName second,ulong count,uint at){
   if(count>16)throw new MissingContract(at,"OWN SSO memcmp extends beyond represented16-byte buffers");
   for(int n=0;n<(int)count;n++){int a=first.Inline00[n].Read(at);int b=second.Inline00[n].Read(at);if(a!=b)return a-b;}return 0;
  }
  public static ControllerStateRegistryNode Lookup078960(ControllerStateTransitionState state,PlayerMoveShortName name,Invocation i){
   At(i,0x078969);var candidateBits=Need(state,i.At).Header290.Snapshot();At(i,0x07896f);var candidate=Node(candidateBits.Read(i.At),i.At);var nodeBits=candidate.Parent08.Snapshot();At(i,0x078973);var node=Node(nodeBits.Read(i.At),i.At);byte nil=node.Nil19.Read(i.At);
   if(nil!=0){At(i,0x0789f1);return candidate;}
   At(i,0x07897e);var length=Need(name,i.At).Length10.Snapshot();At(i,0x07898c);var capacity=name.Capacity18.Snapshot();var visited=new HashSet<ControllerStateRegistryNode>();
   for(;;){
    At(i,0x078994);if(capacity.Read(i.At)>=16){At(i,0x07899a);throw new MissingContract(i.At,"OWN searched-name heap pointer is not represented");}
    At(i,0x0789a2);Node(node,i.At);if(!visited.Add(node))throw new MissingContract(i.At,"Cyclic OWN registry search is outside the finite tree domain");var key=node.Name20;if(key.Capacity18.Read(i.At)>=16){At(i,0x0789ad);throw new MissingContract(i.At,"OWN node-name heap pointer is not represented");}
    At(i,0x0789b0);var keyLength=key.Length10.Snapshot();At(i,0x0789b6);ulong a=keyLength.Read(i.At),b=length.Read(i.At);ulong count=a<b?a:b;int cmp=0;
    if(count!=0){At(i,0x0789c2);cmp=CompareOwnBytes(key,name,count,i.At);}
    if(cmp<0||(cmp==0&&a<b)){At(i,0x0789cd);nodeBits=node.Right10.Snapshot();}
    else{At(i,0x0789f7);candidate=node;At(i,0x0789fa);nodeBits=node.Left00.Snapshot();}
    At(i,0x0789d1);node=Node(nodeBits.Read(i.At),i.At);if(node.Nil19.Read(i.At)!=0){At(i,0x0789f1);return candidate;}
   }
  }
  public static int Compare019E40(PlayerMoveShortName first,PlayerMoveShortName second,Invocation i){
   At(i,0x019e4a);if(Need(second,i.At).Capacity18.Read(i.At)>=16){At(i,0x019e55);throw new MissingContract(i.At,"OWN second-name heap pointer is not represented");}
   At(i,0x019e58);if(Need(first,i.At).Capacity18.Read(i.At)>=16){At(i,0x019e63);throw new MissingContract(i.At,"OWN first-name heap pointer is not represented");}
   At(i,0x019e66);var secondLength=second.Length10.Snapshot();At(i,0x019e69);var firstLength=first.Length10.Snapshot();At(i,0x019e6f);ulong a=firstLength.Read(i.At),b=secondLength.Read(i.At);ulong count=a<b?a:b;
   if(count!=0){At(i,0x019e7b);int cmp=CompareOwnBytes(first,second,count,i.At);if(cmp!=0){At(i,0x019ea9);return cmp;}}
   At(i,0x019e84);if(a<b){At(i,0x019e96);return -1;}At(i,0x019ea9);return a>b?1:0;
  }
  static ControllerStateRegistryNode Node(ControllerStateRegistryNode node,uint at){Need(node,at).RequireLive(at);return node;}
  static ControllerPlayerLifecycleSlots Slots(PlayerMouseState state,uint readAt,uint callAt,Invocation i){ControllerPlayerStateFields.ForBound(state,At(i,readAt));var bits=state.Virtual00.Snapshot();At(i,callAt);return ControllerPlayerLifecycleSlots.ForBound(bits.Read(i.At),i.At);}
  static void Exit(PlayerMouseState state,Invocation i){var slots=Slots(state,0x123dfe,0x123e01,i);uint target=slots.End228.Read(i.At);if(target!=0x015880)throw new MissingContract(i.At,"Unclosed state+228 target0x"+target.ToString("X"));At(i,0x015880);/* exact saved RET0; no fields read or return byte fabricated */}
  static void Entry(PlayerMouseState state,ControllerStateTransitionContext context,ControllerMoveToEntryInvocation call,Invocation i){var slots=Slots(state,0x123edd,0x123ee0,i);uint target=slots.Begin220.Read(i.At);if(target!=0x166020)throw new MissingContract(i.At,"Unclosed state+220 target0x"+target.ToString("X"));MoveToOnBegin166020(state,context,call);}
  static void CopyData(PlayerMoveTransitionPayload source,ControllerStateData40 destination,bool overlay,Invocation i){
   At(i,overlay?0x123ee8u:0x123eadu);Need(source,i.At);var q=source.Zero00.Snapshot();var d8=source.Zero08.Snapshot();var dc=source.Padding0C.Snapshot();
   At(i,overlay?0x123eecu:0x123eb1u);destination.Qword00.CopyFrom(q);destination.Dword08.CopyFrom(d8);destination.Padding0C.CopyFrom(dc);
   At(i,overlay?0x123ef3u:0x123eb8u);var p=source.Destination10;var region=p.Q0.Snapshot();var x=p.D8.Snapshot();var y=p.DC.Snapshot();
   At(i,overlay?0x123ef8u:0x123ebdu);destination.Position10.Q0.CopyFrom(region);destination.Position10.D8.CopyFrom(x);destination.Position10.DC.CopyFrom(y);
   At(i,overlay?0x123effu:0x123ec4u);var z=p.D10.Snapshot();var pad=p.D14.Snapshot();At(i,overlay?0x123f05u:0x123ecau);destination.Position10.D10.CopyFrom(z);destination.Position10.D14.CopyFrom(pad);
  }
  static void CopyPosition(WorldPosition24 from,WorldPosition24 to,Invocation i,uint first,uint store,uint tail,uint last){At(i,first);Need(from,i.At);var q=from.Q0.Snapshot();var x=from.D8.Snapshot();var y=from.DC.Snapshot();At(i,store);to.Q0.CopyFrom(q);to.D8.CopyFrom(x);to.DC.CopyFrom(y);At(i,tail);var z=from.D10.Snapshot();var pad=from.D14.Snapshot();At(i,last);to.D10.CopyFrom(z);to.D14.CopyFrom(pad);}
  public static void SetState123D50(Controller controller,PlayerMoveShortName name,PlayerMoveTransitionPayload payload,ControllerStateTransitionContext context,ControllerStateTransitionInvocation invocation){
   var c=Need(invocation,0x123d68);c.Begin();var i=c.Native;var state=State(controller,context,At(i,0x123d68));At(i,0x123d7b);var candidate=Lookup078960(state,name,i);c.LookupReturned=true;
   At(i,0x123d80);var headerBits=state.Header290.Snapshot();At(i,0x123d87);var header=headerBits.Read(i.At);
   if(!Object.ReferenceEquals(candidate,header)){At(i,0x123d8c);var key=Node(candidate,i.At).Name20;At(i,0x123d93);if(Compare019E40(name,key,i)<0)candidate=header;}
   At(i,0x123d9f);if(Object.ReferenceEquals(candidate,header)){
    At(i,0x123da4);ulong capacity=Need(name,i.At).Capacity18.Read(i.At);if(capacity>=16){At(i,0x123dab);throw new MissingContract(i.At,"Missing-state log heap-name pointer is outside the own SSO name view");}
    At(i,0x123dae);throw new MissingContract(i.At,"Missing-state logging requires actual gEngine import and slot18; no successful log callback");
   }
   At(i,0x123dd2);var oldFlag=state.Changed2D0.Snapshot();At(i,0x123dd9);state.Changed2D0.Write(0);bool overlay=state.Count2F0.Read(At(i,0x123de0))!=0;
   if(!overlay){
    At(i,0x123dee);var previousBits=state.Current2A0.Snapshot();var previous=previousBits.Read(At(i,0x123df5));
    if(previous!=null){
     Exit(previous,i);c.ExitReturned=true;if(state.Changed2D0.Read(At(i,0x123e07))!=0){At(i,0x123f37);c.Returned=true;return;}
     At(i,0x123e17);oldFlag.Write(1);At(i,0x123e1a);var current=GetCurrentStateData125A00(controller,context,c.CurrentData);
     uint incoming=unchecked((uint)Need(payload,At(i,0x123e1f)).Zero00.Read(i.At));if(current.ReadDword00(At(i,0x123e22))!=incoming){
      At(i,0x123e2d);current=GetCurrentStateData125A00(controller,context,c.CurrentData);uint oldId=current.ReadDword00(At(i,0x123e32));At(i,0x123e34);var manager=GameReachedGoalEngineContracts.GetObjectManager01C040(context.Lookup.Engine,i);
      At(i,0x123e3f);throw new MissingContract(0x019190,"Actual019190 target lookup remains unclosed after singleton; captured target="+oldId+", manager="+(manager==null?"null":"nonnull"));
     }
    }
   }
   CopyData(payload,state.Data2A8,overlay,i);c.DataCopied=true;
   At(i,overlay?0x123f0du:0x123ed2u);var next=Node(candidate,i.At).State40.Snapshot();At(i,overlay?0x123f11u:0x123ed6u);state.Current2A0.CopyFrom(next);c.StatePublished=true;
   if(!overlay){var target=next.Read(At(i,0x123edd));Entry(target,context,c.Entry,i);c.EntryReturned=true;}
   At(i,0x123f18);state.Changed2D0.CopyFrom(oldFlag);At(i,0x123f37);c.Returned=true;
  }
  public static void AddState125730(Controller controller,PlayerMoveShortName name,PlayerMouseState incoming,ControllerStateTransitionContext context,ControllerStateTransitionInvocation invocation){
   var c=Need(invocation,0x125745);c.Begin();var i=c.Native;var state=State(controller,context,At(i,0x125745));At(i,0x125755);var candidate=Lookup078960(state,name,i);c.LookupReturned=true;
   At(i,0x12575d);var header=state.Header290.Read(i.At);bool missing=Object.ReferenceEquals(candidate,header);if(!missing){At(i,0x125762);var key=Node(candidate,i.At).Name20;At(i,0x125769);missing=Compare019E40(name,key,i)<0;}if(missing)candidate=state.Header290.Read(At(i,0x125772));
   if(!Object.ReferenceEquals(candidate,state.Header290.Read(At(i,0x125775)))){At(i,0x12577a);var oldBits=Node(candidate,i.At).State40.Snapshot();var old=oldBits.Read(At(i,0x12577e));if(old!=null){ControllerPlayerStateFields.ForBound(old,At(i,0x125783));var table=old.Virtual00.Snapshot();At(i,0x12578b);table.Read(i.At);throw new MissingContract(i.At,"Actual old-state slot0 destruction is unclosed; node+40 was not cleared or replaced");}}
   At(i,0x125793);candidate=Lookup078960(state,name,i);At(i,0x12579b);header=state.Header290.Read(i.At);bool insert=Object.ReferenceEquals(candidate,header);
   if(!insert){At(i,0x1257a0);var key=Node(candidate,i.At).Name20;At(i,0x1257a7);insert=Compare019E40(name,key,i)<0;}
   if(insert){At(i,0x1257c3);throw new MissingContract(0x125f90,"Actual insertion allocation125F90/07B920 is unclosed; no registered node invented");}
   At(i,0x1257b0);Node(candidate,i.At).State40.Write(incoming);c.StatePublished=true;At(i,0x1257ff);c.Returned=true;
  }
  static byte OverlaySelect210(PlayerMouseState state,Invocation i){var slots=Slots(state,0x125a24,0x125a27,i);uint target=slots.Select210.Read(i.At);throw new MissingContract(i.At,"Actual overlay slot210 target0x"+target.ToString("X")+" is unclosed");}
  public static ControllerStateData40 GetCurrentStateData125A00(Controller controller,ControllerStateTransitionContext context,ControllerCurrentDataInvocation invocation){
   var c=Need(invocation,0x125a0a);c.Begin();var i=c.Native;var state=State(controller,context,At(i,0x125a0a));var headerBits=state.Overlay2E8.Snapshot();At(i,0x125a14);var header=Need(headerBits.Read(i.At),i.At);var nodeBits=header.First00.Snapshot();At(i,0x125a17);var node=nodeBits.Read(i.At);var visited=new HashSet<MouseStateNode>();
   while(!ControllerOverlaySentinel.SameAddress(node,header,i.At)){
    var fields=ControllerOverlayNodeFields.ForBound(node,At(i,0x125a20));if(!visited.Add(node))throw new MissingContract(i.At,"Cyclic own overlay walk is outside the finite domain");var selectedBits=node.State10.Snapshot();var selected=selectedBits.Read(At(i,0x125a24));byte value=OverlaySelect210(selected,i);
    At(i,0x125a2d);if(value==0){At(i,0x125a4f);c.Result=fields.Data18;c.OverlaySelected=true;c.Returned=true;At(i,0x125a5d);return c.Result;}
    At(i,0x125a31);nodeBits=fields.Next00.Snapshot();At(i,0x125a34);var freshHeader=state.Overlay2E8.Snapshot();node=nodeBits.Read(i.At);header=Need(freshHeader.Read(i.At),i.At);
   }
   At(i,0x125a3d);c.Result=state.Data2A8;c.Returned=true;At(i,0x125a4e);return c.Result;
  }
  static MoveCharacter Resolve(Field<MoveCharacter> captured,PlayerMouseState state,ControllerStateTransitionContext context,ControllerMoveToEntryInvocation c,uint test,uint ownerAt,uint call,uint store){
   var i=c.Native;var role=captured.Read(At(i,test));if(role!=null)return role;At(i,ownerAt);var owner=state.Owner08.Snapshot();At(i,call);c.LookupCalls++;role=PlayerMousePlayerLookupContracts.Resolve128530(owner.Read(0x128536),context.Lookup,c.Lookup);At(i,store);state.CachedRole10.Write(role);return role;
  }
  static float RunSpeed069040(MoveCharacter role,byte mode,Invocation i){At(i,0x069040);throw new MissingContract(i.At,"Recovered GetRunSpeed069040 still requires actual attribute and virtual-stat producers; no speed field/default/callback substituted");}
  static void Move124340(Controller controller,WorldPosition24 destination,float speed,Invocation i){At(i,0x124340);throw new MissingContract(i.At,"124340 allocation/531A30/082B10/127DD0 production composition is not implemented; no MoveAction or navigation request invented");}
  public static void MoveToOnBegin166020(PlayerMouseState state,ControllerStateTransitionContext context,ControllerMoveToEntryInvocation invocation){
   var c=Need(invocation,0x16602c);c.Begin();var i=c.Native;ControllerPlayerStateFields.ForBound(state,At(i,0x16602c));var ownerBits=state.Owner08.Snapshot();At(i,0x166030);var data=GetCurrentStateData125A00(ownerBits.Read(0x125a0a),context,c.FirstData);
   CopyPosition(data.Position10,c.Destination,i,0x16603a,0x16603e,0x166043,0x166048);At(i,0x16604e);c.RegionToken.CopyFrom(c.Destination.Q0);c.RegionReturned=true;
   if(c.RegionToken.Read(At(i,0x166054))==0){At(i,0x1661b3);c.Returned=true;return;}
   At(i,0x16605d);var cached=state.CachedRole10.Snapshot();var role=Resolve(cached,state,context,c,0x166069,0x16606e,0x166072,0x166077);
   At(i,0x16607e);var entity=context.Lookup.Bridge.ForMoveCharacter(role,0x00bbe0).Owner.Entity;var coords=EngineMouseGetterContracts.GetCoordsUnparented00BBE0(entity,c.Coordinates);c.CoordinatesReturned=true;
   At(i,0x166084);ownerBits=state.Owner08.Snapshot();coords.RequireLive(0x166088);CopyPosition(coords.Position00,c.Source,i,0x166088,0x16608b,0x166090,0x166095);
   At(i,0x16609b);data=GetCurrentStateData125A00(ownerBits.Read(0x125a0a),context,c.SecondData);CopyPosition(data.Position10,c.FreshDestination,i,0x1660af,0x1660b3,0x1660b8,0x1660bd);
   At(i,0x1660c6);var delta=WorldVectorDifferenceContracts.Subtract22C240(c.FreshDestination,c.Delta,c.Source,context.Lookup.Bridge.Callbacks.World.State,context.NullDifference338458,c.Difference);c.DifferenceReturned=true;
   At(i,0x1660cc);var one=context.One7A8054.Snapshot();At(i,0x1660d9);var x=delta.X0.Snapshot();var y=delta.Y4.Snapshot();var fields=ControllerPlayerStateFields.ForBound(state,At(i,0x1660dd));fields.Direction20.X0.CopyFrom(x);fields.Direction20.Y4.CopyFrom(y);At(i,0x1660e2);var z=delta.Z8.Snapshot();At(i,0x1660e5);fields.Direction20.Z8.CopyFrom(z);At(i,0x1660e8);fields.Direction20.Y4.Write(0);
   At(i,0x1660ef);x=fields.Direction20.X0.Snapshot();At(i,0x1660f4);y=fields.Direction20.Y4.Snapshot();At(i,0x1660fc);z=fields.Direction20.Z8.Snapshot();float yy=y.Read(At(i,0x166104));yy=F(yy*yy);float vx=x.Read(At(i,0x16610b));float xx=F(vx*vx);float vz=z.Read(At(i,0x16610f));float zz=F(vz*vz);At(i,0x166113);float squared=F(xx+yy);At(i,0x16611a);squared=F(squared+zz);At(i,0x16611e);float length=F((float)Math.Sqrt(squared));float scale=F(one.Read(At(i,0x166122))/length);At(i,0x166126);vx=F(vx*scale);At(i,0x16612a);float vy=F(y.Read(i.At)*scale);At(i,0x16612e);vz=F(vz*scale);
   At(i,0x166138);fields.Direction20.X0.Write(vx);fields.Direction20.Y4.Write(vy);At(i,0x166147);fields.Direction20.Z8.Write(vz);c.DirectionWritten=true;
   At(i,0x16614a);var prior=GameMovedContracts.ConstructWorldVec228AB0(c.FreshDestination,i);At(i,0x166158);var q=prior.Q0.Snapshot();var px=prior.D8.Snapshot();var py=prior.DC.Snapshot();At(i,0x16615b);fields.Previous30.Q0.CopyFrom(q);fields.Previous30.D8.CopyFrom(px);fields.Previous30.DC.CopyFrom(py);At(i,0x16615f);var pz=prior.D10.Snapshot();var pad=prior.D14.Snapshot();At(i,0x166164);cached=state.CachedRole10.Snapshot();At(i,0x166168);fields.Previous30.D10.CopyFrom(pz);fields.Previous30.D14.CopyFrom(pad);c.PreviousWritten=true;
   role=Resolve(cached,state,context,c,0x16616d,0x166172,0x166176,0x16617b);At(i,0x166184);c.RunSpeedEntered=true;float speed=RunSpeed069040(role,0,c.RunSpeed);
   At(i,0x166189);ownerBits=state.Owner08.Snapshot();At(i,0x1661a6);Move124340(ownerBits.Read(0x12437f),c.Destination,speed,i);At(i,0x1661b3);c.Returned=true;
  }
 }
}
