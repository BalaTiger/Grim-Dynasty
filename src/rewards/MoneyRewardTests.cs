using System;
using GrimDynasty.Rewards;
public static class MoneyRewardTests {
 static int n;
 static WalletSnapshot W(uint value){return new WalletSnapshot{MainPlayerResolved=true,StaticMoneyNameReady=true,Money16C4=value};}
 static void Case(uint initial,int amount,uint expected){var w=W(initial);var r=MoneyRewards.Execute(new MoneyAction{Amount=amount},w);n++;if(r.Status!="Known"||w.Money16C4!=expected)throw new Exception("wallet case "+n+":"+w.Money16C4);}
 static void Is(bool b,string s){n++;if(!b)throw new Exception(s);}
 public static int Main(){try{
  Case(10,75,85);Case(100,-25,75);Case(10,-25,0);Case(100,0,0);Case(0,0,0);Case(1999999990,20,2000000000);Case(2000000000,1,2000000000);Case(4000000000,1000000000,705032704);Case(2147483648,-1,2147483647);Case(2000000000,int.MinValue,0);
  var w=W(100);Is(MoneyRewards.CharacterAddMoney(w,0).Status=="Known"&&w.Money16C4==100,"Character.AddMoney zero differs from action");w=W(uint.MaxValue);Is(MoneyRewards.CharacterAddMoney(w,1).Status=="Known"&&w.Money16C4==0,"character add wrap before cap");
  w=W(5);w.StaticMoneyNameReady=false;Is(MoneyRewards.Execute(new MoneyAction{Amount=3},w).Status=="Unsupported"&&w.Money16C4==5,"unknown static name init no mutation");w.StaticMoneyNameReady=true;w.MainPlayerResolved=null;Is(MoneyRewards.Execute(new MoneyAction{Amount=3},w).Status=="Unsupported"&&w.Money16C4==5,"unknown player");w.MainPlayerResolved=false;w.Money16C4=null;Is(MoneyRewards.Execute(new MoneyAction{Amount=3},w).Status=="Known"&&!w.Money16C4.HasValue,"known missing player no wallet dereference");
  w=W(5);Is(MoneyRewards.Execute(new MoneyAction{Amount=3,Version=1},w).Status=="Unsupported"&&w.Money16C4==5,"uncovered action version");w.Money16C4=null;Is(MoneyRewards.Execute(new MoneyAction{Amount=3},w).Status=="Unsupported","unknown balance");
  Console.WriteLine("{\"status\":\"passed\",\"fixed_assertions\":"+n+"}");return 0;
 }catch(Exception e){Console.Error.WriteLine(e);return 1;}}
}
