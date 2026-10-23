public static class FirstInsertTestProgram {
 public static int Main(){
  int regression=NativeDamageStatisticsTests.Main(new string[0]);
  return regression==0?NativeStatisticsFirstInsertTests.Main():regression;
 }
}
