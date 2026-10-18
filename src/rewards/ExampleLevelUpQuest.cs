using System.Collections.Generic;
using GrimDynasty.Rewards;
public static class ExampleLevelUpQuest {
 public static ExampleExperienceQuest Create(){
  var x=ExampleExperienceQuest.Create(75);var p=x.Player;var w=x.Completion.World;
  p.Level1760=1;p.Experience1764=50;p.NextThreshold=new ThresholdSample{Input=1,FormulaResult=60};
  p.Progression=new LevelProgressionSnapshot{AttributePoints1768=0,SkillPoints176C=0,AttributeGainPerLevel=1,SkillPointSchedule=new List<int>{3},Dirty1699=0,LevelChangeCount17F0=0,MasteriesAllowed=0,MasteryThresholds=new List<uint>{1,10},PetVectorsEmpty=new[]{true,true,true},LevelFxNameLength2320=0,LevelSound22D0NonNull=false};
  p.Progression.FormulaSamples.Add(1,60);p.Progression.FormulaSamples.Add(2,160);p.Progression.FormulaSamples.Add(3,300);
  w.Events=x.Quest.Events;w.EngineInitialized=true;w.Service19B0Present=false;
  w.LevelDisplay=new LevelDisplaySnapshot{Queue=new CinematicTextQueue(),LocalizedTitle="军功晋阶"};w.LevelDisplay.LocalizedReachedText.Add(2,"已升至二级");w.LevelDisplay.LocalizedReachedText.Add(3,"已升至三级");
  x.Quest.Auto.Runtime.World.Level=1;return x;
 }
}
