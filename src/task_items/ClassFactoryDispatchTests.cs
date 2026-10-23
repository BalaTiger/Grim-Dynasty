using System;
using System.Collections.Generic;
using GrimDynasty.TaskItems;
class ClassFactoryDispatchTests {
 static int n;
 static void Need(bool value,string message){n++;if(!value)throw new Exception(message);}
 // ASCII is only a convenience for these original fixture names. Production
 // matching consumes raw bytes and does not interpret any character encoding.
 static byte[] Name(string text){var bytes=new byte[text.Length+1];for(int i=0;i<text.Length;i++)bytes[i]=checked((byte)text[i]);return bytes;}
 static ClassInfoSnapshot Node(string name,int? count,ClassInfoSnapshot[] children,Func<object> constructor){return new ClassInfoSnapshot(Name(name),count,children,constructor);}
 static ClassInfoSnapshot Match(Func<object> constructor){return Node("Item",null,null,constructor);}
 static ClassFactoryResult Create(ClassInfoSnapshot node){return ClassFactoryDispatch.Create(node,Name("Item"));}
 static void Names(){
  var value=new object();var node=Match(delegate{return value;});var result=Create(node);
  Need(result.Status=="Known"&&Object.ReferenceEquals(result.Instance,value)&&result.CallbacksInvoked==1,"self match returns the supplied callback object without requiring child metadata");
  result=ClassFactoryDispatch.Create(Node("Item",0,null,null),Name("item"));
  Need(result.Status=="Known"&&result.Instance==null&&result.CallbacksInvoked==0,"raw names are case sensitive");
  node=new ClassInfoSnapshot(new byte[]{0x80,0xff,0},null,null,delegate{return value;});
  result=ClassFactoryDispatch.Create(node,new byte[]{0x80,0xff,0});
  Need(result.Status=="Known"&&Object.ReferenceEquals(result.Instance,value),"high bytes match without decoding or case conversion");
  result=ClassFactoryDispatch.Create(new ClassInfoSnapshot(new byte[]{0xff,0},0,null,null),new byte[]{0xfe,0});
  Need(result.Status=="Known"&&result.Instance==null,"different high bytes do not match");
  result=ClassFactoryDispatch.Create(new ClassInfoSnapshot(new byte[]{0,255},null,null,delegate{return value;}),new byte[]{0,1});
  Need(result.Status=="Known"&&Object.ReferenceEquals(result.Instance,value),"empty NUL-terminated names match");
  result=ClassFactoryDispatch.Create(new ClassInfoSnapshot(new byte[]{65,0,255},null,null,delegate{return value;}),new byte[]{65,0,1});
  Need(result.Status=="Known"&&Object.ReferenceEquals(result.Instance,value),"bytes after matching NUL are ignored");
  result=ClassFactoryDispatch.Create(Node("ItemLong",0,null,null),Name("Item"));
  Need(result.Status=="Known"&&result.Instance==null,"shorter query does not match longer node name");
  result=ClassFactoryDispatch.Create(Node("Item",0,null,null),Name("ItemLong"));
  Need(result.Status=="Known"&&result.Instance==null,"longer query does not match shorter node name");
  result=ClassFactoryDispatch.Create(new ClassInfoSnapshot(new byte[]{65,0},null,null,delegate{return value;}),new byte[]{65});
  Need(result.Status=="Unsupported"&&result.CallbacksInvoked==0,"missing required query terminator remains unknown");
  result=ClassFactoryDispatch.Create(new ClassInfoSnapshot(new byte[]{65},0,null,null),new byte[]{66});
  Need(result.Status=="Known"&&result.Instance==null,"known early mismatch does not require unobserved trailing bytes");
 }
 static void Traversal(){
  var value=new object();var calls=new List<string>();
  var hidden=Match(delegate{calls.Add("hidden");return value;});
  var matchedNull=Node("Item",1,new[]{hidden},delegate{calls.Add("null");return null;});
  var result=Create(matchedNull);
  Need(result.Status=="Known"&&result.Instance==null&&calls.Count==1&&calls[0]=="null","self match returning null never searches its children");
  calls.Clear();var sibling=Match(delegate{calls.Add("sibling");return value;});
  result=Create(Node("Root",3,new[]{matchedNull,sibling,hidden},null));
  Need(result.Status=="Known"&&Object.ReferenceEquals(result.Instance,value)&&String.Join(",",calls)=="null,sibling"&&result.CallbacksInvoked==2,"parent continues after matched null, skips that matched subtree, and stops at first nonnull sibling");
  calls.Clear();var branch=Node("Branch",2,new[]{matchedNull,sibling},null);
  result=Create(Node("Root",2,new[]{branch,hidden},null));
  Need(result.Status=="Known"&&Object.ReferenceEquals(result.Instance,value)&&String.Join(",",calls)=="null,sibling"&&result.VisitedNodes==4,"child order is depth first before later parent siblings");
  int sharedCalls=0;var shared=Match(delegate{sharedCalls++;return sharedCalls==1?null:value;});
  result=Create(Node("Root",2,new[]{shared,shared},null));
  Need(result.Status=="Known"&&Object.ReferenceEquals(result.Instance,value)&&sharedCalls==2&&result.VisitedNodes==3,"shared node is invoked on each visit rather than globally deduplicated");
  result=Create(Node("Root",int.MaxValue,new[]{sibling},null));
  Need(result.Status=="Known"&&Object.ReferenceEquals(result.Instance,value)&&result.VisitedNodes==2,"early success does not require unused declared child slots");
  result=Create(Node("Root",2,new[]{Node("Other",0,null,null),Node("Else",0,null,null)},null));
  Need(result.Status=="Known"&&result.Instance==null&&result.CallbacksInvoked==0&&result.VisitedNodes==3,"exhausted nonmatching children return known null");
  result=Create(Node("Root",1,new[]{Node("Other",0,null,null),hidden},null));
  Need(result.Status=="Known"&&result.Instance==null&&result.VisitedNodes==2,"child slots beyond signed count are ignored");
 }
 static void Metadata(){
  Need(Create(null).Status=="Unsupported","unknown root is not a known miss");
  Need(ClassFactoryDispatch.Create(Match(delegate{return new object();}),null).Status=="Unsupported","unknown requested name is not an empty name");
  var result=Create(new ClassInfoSnapshot(null,0,null,null));
  Need(result.Status=="Unsupported"&&!result.MayHaveCallbackSideEffects,"missing accessed name bytes are unsupported before callbacks");
  Need(Create(Match(null)).Status=="Unsupported","unknown matched callback differs from a callback returning null");
  result=Create(Node("Other",0,null,null));
  Need(result.Status=="Known"&&result.Instance==null,"nonmatch with zero count requires neither callback nor child array");
  result=Create(Node("Other",int.MinValue,null,delegate{throw new Exception("unvisited constructor");}));
  Need(result.Status=="Known"&&result.Instance==null&&result.CallbacksInvoked==0,"negative signed count skips children and unmatched constructor");
  Need(Create(Node("Other",null,null,null)).Status=="Unsupported","nonmatch needs an observed signed child count");
  Need(Create(Node("Other",1,null,null)).Status=="Unsupported","positive count needs the first visited child slot");
  Need(Create(Node("Other",1,new ClassInfoSnapshot[]{null},null)).Status=="Unsupported","unknown child metadata is not a null constructor result");
  result=Create(Node("Other",2,new[]{Match(delegate{return null;})},null));
  Need(result.Status=="Unsupported"&&result.CallbacksInvoked==1&&result.MayHaveCallbackSideEffects,"missing later child slot is discovered only after prior null callback");
 }
 static void EffectsAndSnapshots(){
  int effects=0;var value=new object();
  var first=Match(delegate{effects++;return null;});
  var unknown=new ClassInfoSnapshot(null,null,null,null);
  var later=Match(delegate{effects+=100;return value;});
  var result=Create(Node("Root",3,new[]{first,unknown,later},null));
  Need(result.Status=="Unsupported"&&result.Instance==null&&result.CallbacksInvoked==1&&result.MayHaveCallbackSideEffects&&effects==1,"unknown metadata after a callback preserves effects and does not continue to later siblings");
  effects=0;var throwing=Match(delegate{effects++;throw new InvalidOperationException("own callback failed");});
  result=Create(Node("Root",2,new[]{throwing,later},null));
  Need(result.Status=="Unsupported"&&result.Reason=="ClassFactoryCallbackThrew:System.InvalidOperationException"&&result.CallbacksInvoked==1&&result.MayHaveCallbackSideEffects&&effects==1,"throwing callback is not reported as a known null or rolled back");
  byte[] raw=Name("Item");var node=new ClassInfoSnapshot(raw,null,null,delegate{return value;});raw[0]=88;
  Need(Object.ReferenceEquals(Create(node).Instance,value),"snapshot copies mutable source name bytes");
  var children=new[]{node};var root=Node("Root",1,children,null);children[0]=unknown;
  Need(Object.ReferenceEquals(Create(root).Instance,value),"snapshot copies mutable source child array");
  byte[] query=Name("Item");first=Match(delegate{query[0]=88;return null;});
  result=ClassFactoryDispatch.Create(Node("Root",2,new[]{first,node},null),query);
  Need(result.Status=="Known"&&Object.ReferenceEquals(result.Instance,value)&&result.CallbacksInvoked==2,"requested name is snapshotted before callback side effects");
 }
 static void Limits(){
  int calls=0;var value=new object();var leaf=Match(delegate{calls++;return value;});
  var result=ClassFactoryDispatch.Create(leaf,Name("Item"),0);
  Need(result.Status=="Unsupported"&&result.Reason.Contains("OutsideCoveredDomain")&&result.VisitedNodes==0&&calls==0,"zero visit budget cannot silently return a native miss");
  result=ClassFactoryDispatch.Create(Node("Root",1,new[]{leaf},null),Name("Item"),1);
  Need(result.Status=="Unsupported"&&result.VisitedNodes==1&&calls==0,"visit budget includes nonmatching parents");
  var first=Match(delegate{calls++;return null;});
  result=ClassFactoryDispatch.Create(Node("Root",2,new[]{first,leaf},null),Name("Item"),2);
  Need(result.Status=="Unsupported"&&result.CallbacksInvoked==1&&result.MayHaveCallbackSideEffects&&calls==1,"budget exhaustion after callback reports possible effects without rollback");
  calls=0;result=ClassFactoryDispatch.Create(Node("Root",1,new[]{leaf},null),Name("Item"),10,1);
  Need(result.Status=="Unsupported"&&result.Reason.Contains("DepthOutsideCoveredDomain")&&calls==0,"depth budget is an explicit coverage limit");
  result=ClassFactoryDispatch.Create(Node("Root",1,new[]{leaf},null),Name("Item"),2,2);
  Need(result.Status=="Known"&&Object.ReferenceEquals(result.Instance,value)&&calls==1,"exact depth and visit limits permit a complete result");
  var chain=leaf;for(int i=0;i<ClassFactoryDispatch.MaximumDepth;i++)chain=Node("Branch",1,new[]{chain},null);
  result=Create(chain);
  Need(result.Status=="Unsupported"&&result.Reason.Contains("DepthOutsideCoveredDomain")&&result.CallbacksInvoked==0,"default finite depth rejects uncovered recursion before stack exhaustion");
  result=ClassFactoryDispatch.Create(leaf,Name("Item"),-1);
  Need(result.Status=="Unsupported"&&result.CallbacksInvoked==0,"negative traversal budget is outside covered domain");
  result=ClassFactoryDispatch.Create(leaf,Name("Item"),ClassFactoryDispatch.MaximumVisits,ClassFactoryDispatch.MaximumDepth+1);
  Need(result.Status=="Unsupported"&&result.CallbacksInvoked==0,"callers cannot extend the declared recursion coverage");
 }
 static int Main(){try{Names();Traversal();Metadata();EffectsAndSnapshots();Limits();Console.WriteLine("{\"status\":\"passed\",\"fixed_assertions\":"+n+",\"scope\":\"Own immutable ClassInfo snapshots and supplied callbacks only; no native constructors, inventory integration, object registration or original execution.\"}");return 0;}catch(Exception e){Console.Error.WriteLine(e);return 1;}}
}
