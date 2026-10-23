using System;
using System.Collections.Generic;
using GrimDynasty.ReverseClient.Actions;

// Small new test draft for the identity bridge only; not yet compiled or run.
// No fake lookup/Stop/controller/event implementations and no prior suites.
public static class GameMovementIdentityBridgeTests {
 static int scenarios,assertions;
 public static int Scenarios{get{return scenarios;}}
 public static int Assertions{get{return assertions;}}
 static void Check(bool value,string why){assertions++;if(!value)throw new Exception(why);}
 static void Equal<T>(T expected,T actual,string why){Check(EqualityComparer<T>.Default.Equals(expected,actual),why);}
 static void Same(object expected,object actual,string why){Check(Object.ReferenceEquals(expected,actual),why);}
 static void Missing(uint at,Action action,string why){try{action();}catch(MissingContract e){Equal(at,e.Address,why);return;}throw new Exception("Expected MissingContract: "+why);}
 static void Reject(Action action,string why){try{action();}catch(InvalidOperationException){assertions++;return;}throw new Exception("Expected binding rejection: "+why);}
 static void Unknown<T>(Field<T> cell){Missing(0,delegate{cell.Read(0);},"Original unknown field retained");}
 static void Test(string name,Action action){try{action();scenarios++;}catch(Exception e){throw new Exception(name+": "+e.Message,e);}}
 sealed class Fixture {
  public readonly MoveCharacter Move=new MoveCharacter();
  public readonly EntityMovementRegistry Entities=new EntityMovementRegistry();
  public readonly GameMovementCallbackContext Callbacks;
  public readonly GameMovementCharacterState Owner;
  public readonly GameMovementIdentityBridge Bridge;
  public Fixture(){
   var character=new AddressSymbol<ClassRecord>(0xa5cba0);character.Write(new ClassRecord());var player=new AddressSymbol<ClassRecord>(0xa5cab0);player.Write(new ClassRecord());
   Callbacks=new GameMovementCallbackContext(new WorldCoordinateBoundaries(new WorldCoordinateState()),Entities,character,player);
   Owner=new GameMovementCharacterState(new EntityMovementState(Move));Callbacks.Attach(Owner);Bridge=new GameMovementIdentityBridge(Callbacks);
  }
 }
 public static void Run(){scenarios=0;assertions=0;
  Test("Existing views bind once and borrow original unknown cells",delegate{
   var f=new Fixture();var view=f.Bridge.Bind(f.Owner,f.Move);Same(view,f.Bridge.Bind(f.Owner,f.Move),"Same pair is idempotent");Same(view,f.Bridge.ForCallback(f.Owner.Interface38,0x0671fd),"Callback direction");Same(view,f.Bridge.ForMoveCharacter(f.Move,0x067208),"Move direction");
   Same(f.Move,view.Owner.Entity.Identity,"New fixture uses existing Move as full identity");Same(f.Move.MovementDC0,view.BorrowMovementDC0(0x0671fd),"Original manager pointer cell");Same(f.Move.AssociatedId16C0,view.BorrowAssociatedId16C0(0x067208),"Original associated ID cell");
   Unknown(f.Move.MovementDC0);Unknown(f.Move.AssociatedId16C0);Unknown(f.Owner.ActionState24B4);Unknown(f.Owner.Entity.Virtual00);Unknown(f.Move.Handler24C0);
  });
  Test("Borrowed cells expose writes in both directions without copying",delegate{
   var f=new Fixture();var view=f.Bridge.Bind(f.Owner,f.Move);var manager=new MovementManager();manager.Moving38.Write(1);f.Move.MovementDC0.Write(manager);
   Same(manager,view.BorrowMovementDC0(0x0671fd).Read(0x067204),"Same existing manager");view.BorrowAssociatedId16C0(0x067208).Write(21);Equal(21u,f.Move.AssociatedId16C0.Read(0),"Borrowed ID write shared");f.Move.AssociatedId16C0.Write(22);Equal(22u,view.BorrowAssociatedId16C0(0x067208).Read(0),"Original ID write shared");
   Equal((byte)1,manager.Moving38.Read(0),"Bridge performs no reached/Stop action");Unknown(manager.Owner00);Unknown(manager.Registered6C);
  });
  Test("Both rebinding directions reject without losing original pair",delegate{
   var f=new Fixture();var view=f.Bridge.Bind(f.Owner,f.Move);var otherMove=new MoveCharacter();var otherOwner=new GameMovementCharacterState(new EntityMovementState(new object()));f.Callbacks.Attach(otherOwner);
   Reject(delegate{f.Bridge.Bind(f.Owner,otherMove);},"Owner cannot gain another Move");Reject(delegate{f.Bridge.Bind(otherOwner,f.Move);},"Move cannot gain another owner");
   Same(view,f.Bridge.ForCallback(f.Owner.Interface38,0x0671fd),"Original owner binding retained");Same(view,f.Bridge.ForMoveCharacter(f.Move,0x067208),"Original Move binding retained");
   var other=f.Bridge.Bind(otherOwner,otherMove);Same(other,f.Bridge.ForCallback(otherOwner.Interface38,0x0671fd),"Failed rebind left other owner available");Same(other,f.Bridge.ForMoveCharacter(otherMove,0x067208),"Failed rebind left other Move available");
  });
  Test("Unregistered unbound and foreign bridge access never creates a role",delegate{
   var f=new Fixture();Missing(0x0671fd,delegate{f.Bridge.ForCallback(f.Owner.Interface38,0x0671fd);},"Registered owner still needs explicit bridge");Missing(0x067208,delegate{f.Bridge.ForMoveCharacter(f.Move,0x067208);},"Move has no implicit default owner");
   var view=f.Bridge.Bind(f.Owner,f.Move);var foreign=new Fixture();Missing(0x0671fd,delegate{foreign.Bridge.ForCallback(f.Owner.Interface38,0x0671fd);},"Foreign context lacks canonical owner");Missing(0x067208,delegate{foreign.Bridge.RequireBound(view,0x067208);},"View belongs to original bridge");
   var unattached=new GameMovementCharacterState(new EntityMovementState(new object()));Missing(0,delegate{f.Bridge.Bind(unattached,new MoveCharacter());},"Setup does not attach owner implicitly");Unknown(f.Move.MovementDC0);Unknown(f.Move.AssociatedId16C0);
  });
  Test("Released Entity blocks both lookup directions and subsequent borrowing",delegate{
   var f=new Fixture();var view=f.Bridge.Bind(f.Owner,f.Move);var manager=new MovementManager();manager.Moving38.Write(1);f.Move.MovementDC0.Write(manager);f.Owner.Entity.MarkReleased();
   Missing(0x0671fd,delegate{f.Bridge.ForCallback(f.Owner.Interface38,0x0671fd);},"Callback checks Entity lifetime");Missing(0x067208,delegate{f.Bridge.ForMoveCharacter(f.Move,0x067208);},"Move lookup checks same Entity lifetime");Missing(0x067204,delegate{view.BorrowMovementDC0(0x067204);},"Retained view rechecks lifetime");Missing(0x067208,delegate{view.BorrowAssociatedId16C0(0x067208);},"ID borrowing rechecks lifetime");Equal((byte)1,manager.Moving38.Read(0),"No hidden manager write on failure");
  });
 }
}
