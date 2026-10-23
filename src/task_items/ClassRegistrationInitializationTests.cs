using System;
using System.Collections.Generic;
using GrimDynasty.TaskItems;
class ClassRegistrationInitializationTests {
 static int assertions,scenarios;
 static void Need(bool value,string why){assertions++;if(!value)throw new Exception(why);}
 static ClassRegistrationChildStorage Storage(params ClassRegistrationNode[] nodes){return new ClassRegistrationChildStorage(nodes);}
 static ClassRegistrationNode Node(ClassRegistrationNode parent,int count,ClassRegistrationChildStorage storage){
  return new ClassRegistrationNode(new byte[]{99,0},parent,count,storage,new object());
 }
 static ClassRegistrationCallbacks Basic(){
  return new ClassRegistrationCallbacks(delegate(byte[] name){return name;},
   delegate(ulong size){return new ClassRegistrationChildStorage(new ClassRegistrationNode[(int)(size/8)]);},
   delegate(ClassRegistrationChildStorage old){},delegate(ulong token){return 0;});
 }
 static bool Bytes(byte[] actual,params byte[] expected){
  if(actual==null||actual.Length!=expected.Length)return false;
  for(int i=0;i<actual.Length;i++)if(actual[i]!=expected[i])return false;
  return true;
 }
 static void NormalWriters(){
  var existing=Node(null,0,null);var actorOld=Storage(existing,null);
  var actor=Node(null,2,actorOld);var itemOld=Storage(existing);
  var item=Node(null,1,itemOld);object creator=item.CreatorIdentity;
  var allocated=Storage(null,null,null);byte[] duplicated=null;var calls=new List<string>();
  bool allocationSawName=false,freeSawPrefix=false,exitSawPublication=false;
  var callbacks=new ClassRegistrationCallbacks(
   delegate(byte[] literal){calls.Add("duplicate");duplicated=literal;return literal;},
   delegate(ulong size){calls.Add("allocate");allocationSawName=size==24&&Object.ReferenceEquals(item.Name,duplicated);return allocated;},
   delegate(ClassRegistrationChildStorage old){calls.Add("free");freeSawPrefix=Object.ReferenceEquals(old,actorOld)&&
    actor.Count==3&&Object.ReferenceEquals(actor.Children,actorOld)&&Object.ReferenceEquals(allocated.Slots[2],item);},
   delegate(ulong token){calls.Add("exit");exitSawPublication=token==0x180604450UL&&
    Object.ReferenceEquals(actor.Children,allocated);return 0;});
  var result=ClassRegistrationInitialization.ApplyItem(item,actor,callbacks);
  Need(result.Status=="Known"&&result.ReturnValue==0&&result.Reason==null&&result.Stage=="Complete","Item completes with the observed outer-helper result");
  Need(String.Join(",",calls.ToArray())=="duplicate,allocate,free,exit","Item callback order is duplicate, allocate, free, exit");
  Need(Object.ReferenceEquals(item.Parent,actor)&&Object.ReferenceEquals(result.SelectedParent,actor)&&result.ParentAssigned,"Item sets then selects its Actor parent");
  Need(Bytes(item.Name,73,116,101,109,0)&&Object.ReferenceEquals(item.Name,duplicated)&&allocationSawName,"Item literal and returned name identity are retained before allocation");
  Need(result.RequestedBytes==24&&result.AllocationCount==2&&result.CopiedChildren==2,"allocation size and copied prefix use the signed count");
  Need(Object.ReferenceEquals(allocated.Slots[0],existing)&&allocated.Slots[1]==null&&Object.ReferenceEquals(allocated.Slots[2],item),"children retain order and known null pointer entries");
  Need(freeSawPrefix&&exitSawPublication&&result.FreeCompleted&&result.StoragePublished,"count changes before free; storage publishes after free and before exit");
  Need(Object.ReferenceEquals(actorOld.Slots[0],existing)&&actorOld.Slots[1]==null&&actorOld.Slots.Length==2,"copying leaves old storage contents unchanged");
  Need(Object.ReferenceEquals(item.CreatorIdentity,creator)&&item.Count==1&&Object.ReferenceEquals(item.Children,itemOld),"registration preserves the node creator and its own child storage");
  Need(result.CallbacksInvoked==4&&result.OwnExitRawReturn==0&&result.MayHaveEffects,"journal records completed own callback effects");
  var quest=Node(item,0,null);object questCreator=quest.CreatorIdentity;ulong seenToken=0;
  var basic=Basic();
  var qr=ClassRegistrationInitialization.ApplyQuestItem(quest,new ClassRegistrationCallbacks(
   basic.DuplicateName,basic.Allocate,basic.Free,delegate(ulong token){seenToken=token;return 0;}));
  Need(qr.Status=="Known"&&!qr.ParentAssigned&&seenToken==0x180605530UL&&Bytes(quest.Name,81,117,101,115,116,73,116,101,109,0),"QuestItem uses its preexisting parent and its own literal/exit identity");
  Need(item.Count==2&&Object.ReferenceEquals(item.Children.Slots[0],existing)&&Object.ReferenceEquals(item.Children.Slots[1],quest)&&
   Object.ReferenceEquals(quest.CreatorIdentity,questCreator)&&Object.ReferenceEquals(qr.FreeArgument,itemOld),"QuestItem appends without replacing creator identity");
  scenarios++;
 }
 static void RepeatedWriters(){
  var actor=Node(null,0,null);var item=Node(null,0,null);var basic=Basic();int duplicateCalls=0,exitCalls=0;
  var callbacks=new ClassRegistrationCallbacks(delegate(byte[] name){duplicateCalls++;return name;},basic.Allocate,basic.Free,
   delegate(ulong token){exitCalls++;return 0;});
  var first=ClassRegistrationInitialization.ApplyItem(item,actor,callbacks);var firstStorage=actor.Children;var firstName=item.Name;
  var second=ClassRegistrationInitialization.ApplyItem(item,actor,callbacks);
  Need(first.Status=="Known"&&second.Status=="Known"&&actor.Count==2,"repeating Item registration appends again");
  Need(Object.ReferenceEquals(actor.Children.Slots[0],item)&&Object.ReferenceEquals(actor.Children.Slots[1],item),"same node identity can occur twice");
  Need(!Object.ReferenceEquals(firstStorage,actor.Children)&&Object.ReferenceEquals(second.FreeArgument,firstStorage),"repeat allocation replaces and frees the prior storage identity");
  Need(duplicateCalls==2&&exitCalls==2&&!Object.ReferenceEquals(firstName,item.Name),"repeat does not suppress duplicate-name or exit registration");
  var quest=Node(item,0,null);var q1=ClassRegistrationInitialization.ApplyQuestItem(quest,callbacks);
  var q2=ClassRegistrationInitialization.ApplyQuestItem(quest,callbacks);
  Need(q1.Status=="Known"&&q2.Status=="Known"&&item.Count==2&&Object.ReferenceEquals(item.Children.Slots[0],quest)&&
   Object.ReferenceEquals(item.Children.Slots[1],quest),"QuestItem also has no deduplication or run-once guard");
  Need(duplicateCalls==4&&exitCalls==4&&actor.Count==2,"repeat QuestItem only appends to its selected parent");
  scenarios++;
 }
 static void ParentAndNamePrefixes(){
  var actor=Node(null,0,null);var alternate=Node(null,0,null);var item=Node(null,0,null);var oldName=item.Name;
  var basic=Basic();bool sawAssignedParent=false;
  var result=ClassRegistrationInitialization.ApplyItem(item,actor,new ClassRegistrationCallbacks(
   delegate(byte[] name){sawAssignedParent=Object.ReferenceEquals(item.Parent,actor)&&Object.ReferenceEquals(item.Name,oldName);item.Parent=alternate;return null;},
   basic.Allocate,basic.Free,basic.RegisterExit));
  Need(sawAssignedParent&&result.Status=="Known"&&item.Name==null,"strdup null is a known stored value and does not stop registration");
  Need(Object.ReferenceEquals(result.SelectedParent,alternate)&&alternate.Count==1&&actor.Count==0,"Item rereads parent after the duplicate callback");
  var quest=Node(actor,0,null);byte[] ownName={0xff,0};
  var qr=ClassRegistrationInitialization.ApplyQuestItem(quest,new ClassRegistrationCallbacks(
   delegate(byte[] name){quest.Parent=alternate;return ownName;},basic.Allocate,basic.Free,basic.RegisterExit));
  Need(qr.Status=="Known"&&Object.ReferenceEquals(qr.SelectedParent,alternate)&&alternate.Count==2,"QuestItem also rereads parent after duplication");
  Need(Object.ReferenceEquals(quest.Name,ownName)&&!qr.ParentAssigned,"returned name bytes are not reinterpreted or copied by the writer");
  var orphan=Node(null,-1,null);int exitCalls=0;
  var orphanResult=ClassRegistrationInitialization.ApplyQuestItem(orphan,new ClassRegistrationCallbacks(
   delegate(byte[] name){return null;},null,null,delegate(ulong token){exitCalls++;return -1;}));
  Need(orphanResult.Status=="Known"&&orphanResult.ReturnValue==-1&&exitCalls==1,"null parent still reaches exit registration and -1 is a normal helper return");
  Need(!orphanResult.RequestedBytes.HasValue&&!orphanResult.ChildAppended&&orphan.Count==-1&&orphan.Children==null,"null parent does not inspect unrelated count or require allocation/free");
  var missing=Node(alternate,0,null);byte[] saved=missing.Name;
  var mr=ClassRegistrationInitialization.ApplyItem(missing,actor,null);
  Need(mr.Reason=="UnknownDuplicateNameCallback"&&mr.ParentAssigned&&Object.ReferenceEquals(missing.Parent,actor),"missing dependency is checked after Item parent assignment");
  Need(Object.ReferenceEquals(missing.Name,saved)&&!mr.NameAssigned&&mr.MayHaveEffects&&mr.CallbacksInvoked==0,"unknown duplication retains the exact completed prefix");
  var thrown=Node(null,0,null);byte[] thrownName=thrown.Name;
  var tr=ClassRegistrationInitialization.ApplyItem(thrown,actor,new ClassRegistrationCallbacks(
   delegate(byte[] name){thrown.Parent=alternate;throw new InvalidOperationException();},null,null,null));
  Need(tr.Stage=="DuplicateName"&&tr.Reason=="OwnClassRegistrationOperationThrew:System.InvalidOperationException"&&Object.ReferenceEquals(thrown.Parent,alternate),"own duplicate exception preserves its side effect");
  Need(Object.ReferenceEquals(thrown.Name,thrownName)&&!tr.NameAssigned&&tr.CallbacksInvoked==1,"duplicate exception does not invent a returned name");
  scenarios++;
 }
 static void OwnFailurePrefixes(){
  var basic=Basic();var parent=Node(null,0,null);var node=Node(parent,0,null);
  var allocation=ClassRegistrationInitialization.ApplyQuestItem(node,new ClassRegistrationCallbacks(
   basic.DuplicateName,delegate(ulong size){throw new InvalidOperationException();},basic.Free,basic.RegisterExit));
  Need(allocation.Stage=="Allocate"&&allocation.NameAssigned&&allocation.Reason=="OwnClassRegistrationOperationThrew:System.InvalidOperationException","own allocation exception keeps the assigned name");
  Need(parent.Count==0&&parent.Children==null&&!allocation.StoragePublished&&!allocation.ExitInvoked,"allocation exception has no later modeled stores");
  var nullAllocation=ClassRegistrationInitialization.ApplyQuestItem(node,new ClassRegistrationCallbacks(
   basic.DuplicateName,delegate(ulong size){return null;},basic.Free,basic.RegisterExit));
  Need(nullAllocation.Reason=="UnsupportedAllocationContract"&&nullAllocation.NameAssigned&&nullAllocation.CallbacksInvoked==2,"null allocation is outside normal allocation contract");
  var missingAllocation=ClassRegistrationInitialization.ApplyQuestItem(node,new ClassRegistrationCallbacks(basic.DuplicateName,null,null,null));
  Need(missingAllocation.Reason=="UnknownClassRegistrationAllocateCallback"&&missingAllocation.RequestedBytes==8&&missingAllocation.NameAssigned,"missing allocation is a lazy post-name boundary");
  var old=Storage();parent.Children=old;var created=Storage((ClassRegistrationNode)null);
  var freeFailure=ClassRegistrationInitialization.ApplyQuestItem(node,new ClassRegistrationCallbacks(
   basic.DuplicateName,delegate(ulong size){return created;},delegate(ClassRegistrationChildStorage value){throw new ApplicationException();},basic.RegisterExit));
  Need(freeFailure.Stage=="FreeOldStorage"&&freeFailure.Reason=="OwnClassRegistrationOperationThrew:System.ApplicationException"&&freeFailure.FreeInvoked&&!freeFailure.FreeCompleted,"free exception is an own interruption, not a native recoverable failure");
  Need(parent.Count==1&&Object.ReferenceEquals(parent.Children,old)&&Object.ReferenceEquals(created.Slots[0],node)&&!freeFailure.StoragePublished&&!freeFailure.ExitInvoked,"free interruption retains incremented count and unpublished new storage");
  parent=Node(null,0,null);node=Node(parent,0,null);
  var missingFree=ClassRegistrationInitialization.ApplyQuestItem(node,new ClassRegistrationCallbacks(basic.DuplicateName,basic.Allocate,null,basic.RegisterExit));
  Need(missingFree.Reason=="UnknownClassRegistrationFreeCallback"&&parent.Count==1&&parent.Children==null&&missingFree.ChildAppended&&!missingFree.FreeInvoked,"missing free preserves the append/count prefix");
  parent=Node(null,0,null);node=Node(parent,0,null);
  var exitFailure=ClassRegistrationInitialization.ApplyQuestItem(node,new ClassRegistrationCallbacks(
   basic.DuplicateName,basic.Allocate,basic.Free,delegate(ulong token){throw new InvalidOperationException();}));
  Need(exitFailure.Stage=="RegisterExit"&&exitFailure.ExitInvoked&&exitFailure.Reason=="OwnClassRegistrationOperationThrew:System.InvalidOperationException","own exit exception is recorded at its stage");
  Need(parent.Count==1&&Object.ReferenceEquals(parent.Children.Slots[0],node)&&exitFailure.StoragePublished&&!exitFailure.ReturnValue.HasValue,"exit exception cannot roll back published children");
  parent=Node(null,0,null);node=Node(parent,0,null);
  var minusOne=ClassRegistrationInitialization.ApplyQuestItem(node,new ClassRegistrationCallbacks(
   basic.DuplicateName,basic.Allocate,basic.Free,delegate(ulong token){return -1;}));
  Need(minusOne.Status=="Known"&&minusOne.ReturnValue==-1&&minusOne.OwnExitRawReturn==-1&&minusOne.StoragePublished&&
   parent.Count==1&&Object.ReferenceEquals(parent.Children.Slots[0],node),"normal outer-helper minus one retains the completed nonempty-parent publication");
  var orphan=Node(null,0,null);
  var odd=ClassRegistrationInitialization.ApplyQuestItem(orphan,new ClassRegistrationCallbacks(
   basic.DuplicateName,null,null,delegate(ulong token){return 17;}));
  Need(odd.Status=="Unsupported"&&odd.Reason=="UnsupportedExitRegistrationContract"&&odd.OwnExitRawReturn==17&&!odd.ReturnValue.HasValue,"outer-helper positive raw return is outside contract but retained");
  var negative=ClassRegistrationInitialization.ApplyQuestItem(orphan,new ClassRegistrationCallbacks(
   basic.DuplicateName,null,null,delegate(ulong token){return -2;}));
  Need(negative.Reason=="UnsupportedExitRegistrationContract"&&negative.OwnExitRawReturn==-2&&negative.NameAssigned,"only exactly -1 is a normal negative outer-helper result");
  var missingExit=ClassRegistrationInitialization.ApplyQuestItem(orphan,new ClassRegistrationCallbacks(basic.DuplicateName,null,null,null));
  Need(missingExit.Reason=="UnknownClassRegistrationExitCallback"&&missingExit.NameAssigned&&!missingExit.ExitInvoked,"unknown exit dependency retains name prefix");
  var absent=ClassRegistrationInitialization.ApplyItem(null,parent,basic);
  Need(absent.Reason=="UnknownClassRegistrationNode"&&!absent.MayHaveEffects&&absent.CallbacksInvoked==0,"unknown target cannot mutate any modeled node");
  scenarios++;
 }
 static void CountAndStorageBoundaries(){
  Need(ClassRegistrationInitialization.AllocationSize(0)==8&&ClassRegistrationInitialization.AllocationSize(1)==16,"ordinary allocation requests include the appended pointer");
  Need(ClassRegistrationInitialization.AllocationSize(-1)==0&&ClassRegistrationInitialization.AllocationSize(int.MaxValue)==ulong.MaxValue,"count increment wraps in 32 bits before sign extension and unsigned multiply");
  Need(ClassRegistrationInitialization.AllocationSize(int.MinValue)==ulong.MaxValue&&ClassRegistrationInitialization.AllocationSize(-2)==ulong.MaxValue&&
   ClassRegistrationInitialization.AllocationSize(int.MaxValue-1)==17179869176UL,"negative unsigned multiplicands saturate on multiplication overflow");
  var basic=Basic();var parent=Node(null,-1,null);var node=Node(parent,0,null);ulong requested=99;
  var negative=ClassRegistrationInitialization.ApplyQuestItem(node,new ClassRegistrationCallbacks(
   basic.DuplicateName,delegate(ulong size){requested=size;return Storage();},basic.Free,basic.RegisterExit));
  Need(requested==0&&negative.NameAssigned&&negative.AllocatedStorage!=null&&negative.Reason=="ClassRegistrationNegativeCountOutsideCoveredDomain","negative count coverage check follows name and allocation prefixes");
  parent.Count=int.MaxValue;
  var excessive=ClassRegistrationInitialization.ApplyQuestItem(node,new ClassRegistrationCallbacks(
   basic.DuplicateName,delegate(ulong size){requested=size;return Storage();},basic.Free,basic.RegisterExit));
  Need(requested==ulong.MaxValue&&excessive.NameAssigned&&excessive.Reason=="ClassRegistrationCountBudgetOutsideCoveredDomain"&&parent.Count==int.MaxValue,"large count request is observed before the own bounded-state stop");
  var child=Node(null,0,null);parent=Node(null,2,Storage(child));node=Node(parent,0,null);var allocated=Storage(null,null,null);
  var shortOld=ClassRegistrationInitialization.ApplyQuestItem(node,new ClassRegistrationCallbacks(
   basic.DuplicateName,delegate(ulong size){return allocated;},basic.Free,basic.RegisterExit));
  Need(shortOld.Reason=="UnknownClassRegistrationOldChildSlot"&&shortOld.CopiedChildren==1&&Object.ReferenceEquals(allocated.Slots[0],child),"short old span retains already copied children");
  Need(parent.Count==2&&!shortOld.ChildAppended&&!shortOld.FreeInvoked,"short old span stops before append and count increment");
  parent=Node(null,1,Storage(child));node=Node(parent,0,null);allocated=Storage((ClassRegistrationNode)null);
  var shortNew=ClassRegistrationInitialization.ApplyQuestItem(node,new ClassRegistrationCallbacks(
   basic.DuplicateName,delegate(ulong size){return allocated;},basic.Free,basic.RegisterExit));
  Need(shortNew.Reason=="ClassRegistrationAppendOutsideCoveredDomain"&&shortNew.CopiedChildren==1&&parent.Count==1&&!shortNew.ChildAppended,"copy can complete before the missing append slot is discovered");
  var sharedSlots=new ClassRegistrationNode[]{child,null};var shared=Storage(sharedSlots);
  parent=Node(null,1,shared);node=Node(parent,0,null);
  var alias=ClassRegistrationInitialization.ApplyQuestItem(node,new ClassRegistrationCallbacks(
   basic.DuplicateName,delegate(ulong size){return new ClassRegistrationChildStorage(sharedSlots);},basic.Free,basic.RegisterExit));
  Need(alias.Reason=="ClassRegistrationAllocationAliasOutsideCoveredDomain"&&alias.NameAssigned&&!alias.ChildAppended,"new wrapper around old slots is still allocation aliasing outside the contract");
  var firstOld=Storage(child,child);var replacementOld=Storage((ClassRegistrationNode)null);
  parent=Node(null,2,firstOld);node=Node(parent,0,null);allocated=Storage(null,null,null);bool freedReplacement=false;
  var reread=ClassRegistrationInitialization.ApplyQuestItem(node,new ClassRegistrationCallbacks(
   basic.DuplicateName,delegate(ulong size){requested=size;parent.Count=1;parent.Children=replacementOld;return allocated;},
   delegate(ClassRegistrationChildStorage old){freedReplacement=Object.ReferenceEquals(old,replacementOld);},basic.RegisterExit));
  Need(reread.Status=="Known"&&requested==24&&reread.AllocationCount==2&&reread.CopiedChildren==1,"copy rereads count after allocation rather than reusing the request count");
  Need(allocated.Slots[0]==null&&Object.ReferenceEquals(allocated.Slots[1],node)&&parent.Count==2&&freedReplacement,"copy and free use the selected parent's current storage after allocation");
  Need(Object.ReferenceEquals(parent.Children,allocated)&&Object.ReferenceEquals(firstOld.Slots[0],child),"new storage publishes while untouched original storage retains identity");
  scenarios++;
 }
 static void ExitBitContract(){
  const ulong token=0x180604450UL;
  var success=ClassRegistrationInitialization.EvaluateExit(0,ulong.MaxValue,token,0);
  Need(success.DecodedValue==ulong.MaxValue&&success.UsesCrtAtexit&&success.ReturnedCallback==token&&success.OuterReturn==0,"sentinel branch with zero raw EAX returns the callback identity then outer zero");
  var positive=ClassRegistrationInitialization.EvaluateExit(0,ulong.MaxValue,token,7);
  Need(positive.ReturnedCallback==0&&positive.OuterReturn==-1,"positive nonzero raw registration EAX fails");
  var negative=ClassRegistrationInitialization.EvaluateExit(0,ulong.MaxValue,token,-7);
  Need(negative.ReturnedCallback==0&&negative.OuterReturn==-1,"negative nonzero raw registration EAX also fails");
  var minimum=ClassRegistrationInitialization.EvaluateExit(0,ulong.MaxValue,token,int.MinValue);
  var maximum=ClassRegistrationInitialization.EvaluateExit(0,ulong.MaxValue,token,int.MaxValue);
  Need(minimum.ReturnedCallback==0&&minimum.OuterReturn==-1&&maximum.ReturnedCallback==0&&maximum.OuterReturn==-1,"both signed EAX extremes are nonzero failures");
  var nullToken=ClassRegistrationInitialization.EvaluateExit(0,ulong.MaxValue,0,0);
  Need(nullToken.ReturnedCallback==0&&nullToken.OuterReturn==-1,"zero callback identity yields outer minus one even with zero raw EAX");
  var nonSentinel=ClassRegistrationInitialization.EvaluateExit(0,0,token,0);
  Need(!nonSentinel.UsesCrtAtexit&&nonSentinel.ReturnedCallback==token&&nonSentinel.OuterReturn==0,"non-sentinel branch uses its supplied raw result without executing either API");
  var one=ClassRegistrationInitialization.EvaluateExit(1,0,token,0);
  Need(one.DecodedValue==0x8000000000000000UL,"ROR by one moves low bit to high bit");
  var sixtyThree=ClassRegistrationInitialization.EvaluateExit(63,0x800000000000003fUL,token,0);
  Need(sixtyThree.DecodedValue==1,"ROR by 63 preserves exact unsigned bit positions");
  var sixtyFour=ClassRegistrationInitialization.EvaluateExit(64,0x0123456789abcdafUL,token,0);
  Need(sixtyFour.DecodedValue==0x0123456789abcdefUL,"cookie low-six-bit zero is a zero rotation");
  var sixtyFive=ClassRegistrationInitialization.EvaluateExit(65,0x0123456789abcdaeUL,token,0);
  Need(sixtyFive.DecodedValue==0x8091a2b3c4d5e6f7UL,"cookie low-six-bit one rotates a nontrivial value");
  var rotatedSentinel=ClassRegistrationInitialization.EvaluateExit(63,0xffffffffffffffc0UL,token,0);
  Need(rotatedSentinel.DecodedValue==ulong.MaxValue&&rotatedSentinel.UsesCrtAtexit,"sentinel comparison follows XOR and rotation");
  scenarios++;
 }
 public static int Main(){
  try{
   NormalWriters();RepeatedWriters();ParentAndNamePrefixes();OwnFailurePrefixes();CountAndStorageBoundaries();ExitBitContract();
   Console.WriteLine("PASS: "+assertions+" assertions, "+scenarios+" scenario groups");return 0;
  }catch(Exception e){Console.Error.WriteLine("FAIL after "+assertions+" assertions: "+e);return 1;}
 }
}
