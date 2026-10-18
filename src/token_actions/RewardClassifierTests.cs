using System;
using GrimDynasty.TokenActions;
public static class RewardClassifierTests {
 static int count;
 static void Check(int?[] list,bool? expected){count++;var r=RewardClassifier.HasRewardActions(list);if(r.Value!=expected||(expected.HasValue?r.Status!="Known":r.Status!="Unsupported"))throw new Exception("reward assertion "+count);}
 public static int Main(){try{
  Check(null,null);Check(new int?[]{},false);
  bool[] expected={false,false,false,false,true,true,true,true,true,false,false,false,false,false,false,true,false,false,true,false,true,false,false,false,false,true,true,false,false,false,false,false};
  for(int i=0;i<expected.Length;i++)Check(new int?[]{i},expected[i]);
  Check(new int?[]{int.MinValue,-1,int.MaxValue},false);
  Check(new int?[]{9,10,1,2,3,19},false);
  Check(new int?[]{9,10,25},true);
  Check(new int?[]{4,null},true);Check(new int?[]{null,4},null);
  Console.WriteLine("{\"status\":\"passed\",\"fixed_assertions\":"+count+"}");return 0;
 }catch(Exception e){Console.Error.WriteLine(e);return 1;}}
}
