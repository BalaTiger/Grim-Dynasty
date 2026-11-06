using System;

namespace GrimDynasty.ReverseClient.Actions {
 public sealed class GameLuaProviderRegistrationInvocation {
  public readonly Invocation Native=new Invocation();
  public uint Comparisons;
  public bool Duplicate,GrowthCalled,ElementStored,EndPublished,Returned;
  public NavigationCrowdPointerAddress<GameLuaProvider> CapturedSearchBegin;
 }
 public sealed class GameLuaProviderLoopInvocation {
  public readonly Invocation Native=new Invocation();
  public long? InitialCount;
  public GameLuaProvider FirstProvider;
  public object FirstVirtualTable;
  public bool ReachedRoleBranches;
 }
 public static class GameLuaProviderContracts {
  static bool Same(NavigationCrowdPointerAddress<GameLuaProvider> a,NavigationCrowdPointerAddress<GameLuaProvider> b){return a==null?b==null:b!=null&&Object.ReferenceEquals(a.Buffer,b.Buffer)&&a.Index==b.Index;}
  // OWN storage proof only. Unknown, null, expired, and distinct allocations
  // remain different; invalid pointer arithmetic is never normalized to empty.
  static long Span(NavigationCrowdPointerAddress<GameLuaProvider> end,NavigationCrowdPointerAddress<GameLuaProvider> begin,uint at){
   if(end==null&&begin==null)return 0;
   if(end==null||begin==null||!Object.ReferenceEquals(end.Buffer,begin.Buffer))throw new MissingContract(at,"Provider span lacks one OWN storage identity");
   end.Buffer.RequireLive(at);
   long bytes=((long)end.Index-begin.Index)*8;
   if(bytes<0)throw new MissingContract(at,"Negative provider span is outside the supported OWN storage domain");
   return bytes>>3;
  }
  // Exact reserve-one wrapper; reuse the already verified stride8 allocation,
  // copy, old-storage release, and capacity/end/begin publication implementation.
  static void ReserveOne058190(NavigationCrowdPointerVector<GameLuaProvider> v,Invocation i){
   var capBits=v.Capacity10.Snapshot();var endBits=v.End08.Snapshot();
   i.At=0x0581a2;var cap=capBits.Read(i.At);var end=endBits.Read(i.At);
   ulong available=unchecked((ulong)Span(cap,end,i.At));if(available>=1)return;
   i.At=0x0581bc;var begin=v.Begin00.Read(i.At);ulong count=unchecked((ulong)Span(end,begin,i.At));
   const ulong maximum=0x1ffffffffffffffful;
   if(unchecked(maximum-count)<1)throw new MissingContract(i.At=0x0581d6,"Native provider vector length_error is outside OWN allocation support");
   i.At=0x0581dd;ulong capacity=unchecked((ulong)Span(cap,begin,i.At)),required=unchecked(count+1),half=capacity>>1;
   ulong candidate=unchecked(maximum-half)>=capacity?unchecked(capacity+half):0;
   if(candidate>=required)required=candidate;
   i.At=0x05820a;NavigationCrowdStateContracts.Reallocate034FE0(v,required,i);
  }
  // The native by-value parameter is spilled to a fresh stack local. This OWN
  // local cannot alias a declared managed heap buffer. Native alias arm and
  // allocator/EH equivalence are outside this finite adaptation.
  public static void RegisterProvider0F3190(GameReachedGoalLuaService service,GameLuaProvider provider,GameLuaProviderRegistrationInvocation call){
   if(call==null)throw new ArgumentNullException("invocation");var i=call.Native;
   call.Comparisons=0;call.Duplicate=false;call.GrowthCalled=false;call.ElementStored=false;call.EndPublished=false;call.Returned=false;call.CapturedSearchBegin=null;
   i.At=0x0f319f;if(service==null)throw new MissingContract(i.At,"Missing same Lua service identity");
   var v=service.Providers40;v.RequireLive(i.At);var end=v.End08.Read(i.At);
   i.At=0x0f31a7;var begin=v.Begin00.Read(i.At);
   i.At=0x0f31ad;long count=Span(end,begin,i.At);
   if(count!=0){
    i.At=0x0f31b9;begin=v.Begin00.Read(i.At);call.CapturedSearchBegin=begin;uint index=0;
    do{
     i.At=0x0f31c0;if(begin==null)throw new MissingContract(i.At,"Nonempty provider search has a null base");
     long slot=(long)begin.Index+index;if(slot>Int32.MaxValue)throw new MissingContract(i.At,"Provider index exceeds bounded OWN storage");
     var value=begin.Buffer.At((int)slot,i.At).Read(i.At);call.Comparisons++;
     if(Object.ReferenceEquals(value,provider)){call.Duplicate=true;call.Returned=true;i.At=0x0f3261;return;}
     i.At=0x0f31ca;end=v.End08.Read(i.At);index=unchecked(index+1);
     i.At=0x0f31d1;count=Span(end,begin,i.At);
     i.At=0x0f31db;if((ulong)index>=unchecked((ulong)count))break;
    }while(true);
   }
   i.At=0x0f31e0;end=v.End08.Read(i.At);
   i.At=0x0f3238;var capacity=v.Capacity10.Read(i.At);
   if(Same(end,capacity)){i.At=0x0f3241;call.GrowthCalled=true;ReserveOne058190(v,i);}
   i.At=0x0f3246;end=v.End08.Read(i.At);
   if(end!=null){i.At=0x0f324f;end.Buffer.At(end.Index,i.At).Write(provider);call.ElementStored=true;}
   i.At=0x0f3252;end=v.End08.Read(i.At);
   if(end==null||end.Index>=end.Buffer.Cells.Length)throw new MissingContract(i.At,"Native null/out-of-range end increment is outside OWN address support");
   end.Buffer.RequireLive(i.At);v.End08.Write(new NavigationCrowdPointerAddress<GameLuaProvider>(end.Buffer,end.Index+1));call.EndPublished=true;
   i.At=0x0f3261;call.Returned=true;
  }
  // Only the proven empty route and prefix before the first provider call.
  // No callback mapping is supplied to manufacture a completed provider loop.
  public static void InitializeProvidersPrefix0F2EDF(GameReachedGoalLuaService service,GameLuaProviderLoopInvocation call){
   if(call==null)throw new ArgumentNullException("invocation");
   var i=call.Native;call.InitialCount=null;call.FirstProvider=null;call.FirstVirtualTable=null;call.ReachedRoleBranches=false;i.At=0x0f2edf;
   if(service==null)throw new MissingContract(i.At,"Missing same Lua service identity");
   i.At=0x0f2ee1;service.Providers40.RequireLive(i.At);var end=service.Providers40.End08.Read(i.At);
   i.At=0x0f2ee5;var begin=service.Providers40.Begin00.Read(i.At);
   i.At=0x0f2ee9;call.InitialCount=Span(end,begin,i.At);
   if(call.InitialCount==0){i.At=0x0f2f25;call.ReachedRoleBranches=true;return;}
   i.At=0x0f2f00;begin=service.Providers40.Begin00.Read(i.At);
   i.At=0x0f2f04;
   if(begin==null)throw new MissingContract(i.At,"Null provider base has no native null guard");
   call.FirstProvider=begin.Buffer.At(begin.Index,i.At).Read(i.At);
   i.At=0x0f2f08;if(call.FirstProvider==null)throw new MissingContract(i.At,"Null provider object has no native null guard");
   call.FirstVirtualTable=call.FirstProvider.Virtual00.Read(i.At);
   i.At=0x0f2f0e;throw new MissingContract(i.At,"Provider vslot0 identity/body is not implemented; no callback or loop completion is assumed");
  }
 }
}
