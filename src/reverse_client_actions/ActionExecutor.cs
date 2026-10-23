using System;
using System.Collections.Generic;

namespace GrimDynasty.ReverseClient.Actions {
 // Instruction-order projection of 089AC0,089BF0,073BA0 and the known part of
 // 07A6D0. The allocator079CD0 and concrete action virtuals stay explicit.
 public static class Executor {
  private static Node Live(Node n,uint at){if(n==null||n.Freed)throw new MissingContract(at,"Invalid own list node");return n;}
  public static Outcome Run(Handler h,ActionRecord a,Counter budget,Boundaries b){
   var i=new Invocation();try{Execute(h,a,budget,b,i);return new Outcome{ReturnedNormally=true};}
   catch(MissingContract x){return new Outcome{Address=x.Address,Boundary=x.Message,Exception=x};}
   catch(Exception x){return new Outcome{Address=i.At,Boundary="External callback did not return normally; native EH unmodeled",Exception=x};}
  }
  public static void Stop(Handler h,Boundaries b,Invocation i){
   ActionRecord a=h.Current08.Read(0x089bf9);if(a==null)return;
   i.At=0x089c05;b.Stop18(a);
   a=h.Current08.Read(0x089c08);if(a!=null){i.At=0x089c19;b.Destroy0(a,1);}
   h.Current08.Write(null);
  }
  public static void Clear(Handler h,Boundaries b,Invocation i){
   Node head=Live(h.Head20.Read(0x073ba6),0x073bac),n=head.Next00.Read(0x073bac);
   head.Next00.Write(head);
   head=Live(h.Head20.Read(0x073bb2),0x073bb5);head.Previous08.Write(head);h.Count28.Write(0);
   var seen=new HashSet<Node>();
   while(!Object.ReferenceEquals(n,h.Head20.Read(0x073bc1))){
    n=Live(n,0x073bd0);if(!seen.Add(n))throw new MissingContract(0x073bd0,"Cyclic malformed list outside supported domain");
    Node next=n.Next00.Read(0x073bd0);i.At=0x073bd3;b.ReleaseNode(n);n=next;
   }
  }
  public static void Execute(Handler h,ActionRecord incoming,Counter budget,Boundaries b,Invocation i){
   // Null action deliberately does not read h or budget.
   ActionRecord action=incoming;if(action==null)return;
   for(;;){
    int remaining=budget.Value.Read(0x089ae6);if(remaining<=0){i.At=0x089bd6;Clear(h,b,i);return;}
    budget.Value.Write(unchecked(remaining-1));
    byte busy=h.Busy18.Read(0x089af4);
    if(busy!=0){
     Node head=Live(h.Head20.Read(0x089b73),0x089b7f),previous=head.Previous08.Read(0x089b7f);
     // 07A6D0 forwards captured links to079CD0 then copies the borrowed
     // input pointer into returned node+10. No fabricated allocation success.
     var borrowedInputSlot=Field<ActionRecord>.Of(action);
     i.At=0x07a6d9;Node node=Live(b.AllocateLinks(h,head,previous,borrowedInputSlot),0x07a6de);
     node.Value10.Write(borrowedInputSlot.Read(0x07a6e7));
     ulong count=h.Count28.Read(0x089b95);
     if(unchecked(0x0aaaaaaaaaaaaaa9UL-count)<1UL){i.At=0x089ba9;b.LengthError();throw new MissingContract(0x089baf,"length_error returned: native INT3 not emulated");}
     h.Count28.Write(unchecked(count+1));head.Previous08.Write(node);
     previous=Live(node.Previous08.Read(0x089bbb),0x089bbf);previous.Next00.Write(node);return;
    }
    ActionRecord old=h.Current08.Read(0x089afa);
    h.Busy18.Write(1);
    if(old!=null){i.At=0x089b08;Stop(h,b,i);}
    h.Current08.Write(action);i.At=0x089b17;b.Start08(action);
    h.Busy18.Write(0);
    if(h.Count28.Read(0x089b1e)==0)return;
    Node headNow=Live(h.Head20.Read(0x089b29),0x089b2d);
    Node first=Live(headNow.Next00.Read(0x089b2d),0x089b30);
    action=first.Value10.Read(0x089b30);
    headNow=Live(h.Head20.Read(0x089b34),0x089b38);first=Live(headNow.Next00.Read(0x089b38),0x089b3b);
    Node previousNode=Live(first.Previous08.Read(0x089b3b),0x089b42);
    Node nextNode=first.Next00.Read(0x089b3f);previousNode.Next00.Write(nextNode);
    nextNode=Live(first.Next00.Read(0x089b45),0x089b4c);previousNode=first.Previous08.Read(0x089b48);nextNode.Previous08.Write(previousNode);
    h.Count28.Write(unchecked(h.Count28.Read(0x089b50)-1));
    i.At=0x089b54;b.ReleaseNode(first);if(action==null)return;
   }
  }
 }
}
