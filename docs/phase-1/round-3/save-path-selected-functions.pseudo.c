/* Ghidra pseudocode; not original source. No auto-analysis. Types/signatures may be inaccurate. */

/* 180593560 save_base_source */

undefined8 * FUN_180593560(undefined8 *param_1)

{
  longlong lVar1;
  ulonglong uVar2;
  int iVar3;
  longlong lVar4;
  undefined8 *puVar6;
  ulonglong uVar7;
  ulonglong uVar8;
  undefined4 uVar9;
  char acStack_118 [272];
  longlong lVar5;
  
  uVar8 = 0;
  uVar9 = 0;
  iVar3 = SHGetSpecialFolderPathA(0,acStack_118,5,0,0);
  if (iVar3 != 0) {
    lVar5 = -1;
    do {
      lVar4 = lVar5 + 1;
      lVar1 = lVar5 + 1;
      lVar5 = lVar4;
    } while (acStack_118[lVar1] != '\0');
    if (lVar4 != 0) goto LAB_1805935c2;
  }
  SHGetSpecialFolderPathA(0,acStack_118,5,0,uVar9);
LAB_1805935c2:
  param_1[3] = 0xf;
  param_1[2] = 0;
  *(undefined1 *)param_1 = 0;
  uVar2 = 0xffffffffffffffff;
  uVar7 = uVar8;
  while (acStack_118[0] != '\0') {
    uVar7 = uVar2 + 1;
    acStack_118[0] = acStack_118[uVar2 + 1];
    uVar2 = uVar7;
  }
  FUN_180016460(param_1,acStack_118,uVar7);
  if (param_1[2] != 0) {
    do {
      puVar6 = param_1;
      if (0xf < (ulonglong)param_1[3]) {
        puVar6 = (undefined8 *)*param_1;
      }
      if (*(char *)(uVar8 + (longlong)puVar6) == '/') {
        puVar6 = param_1;
        if (0xf < (ulonglong)param_1[3]) {
          puVar6 = (undefined8 *)*param_1;
        }
        *(undefined1 *)(uVar8 + (longlong)puVar6) = 0x5c;
      }
      uVar8 = (ulonglong)((int)uVar8 + 1);
    } while (uVar8 < (ulonglong)param_1[2]);
  }
  return param_1;
}


/* 180301350 string_combine_A */

undefined1 * FUN_180301350(undefined1 *param_1,undefined8 param_2,char *param_3)

{
  undefined8 uVar1;
  longlong lVar2;
  
  if (*param_3 == '\0') {
    lVar2 = 0;
  }
  else {
    lVar2 = -1;
    do {
      lVar2 = lVar2 + 1;
    } while (param_3[lVar2] != '\0');
  }
  uVar1 = FUN_180089e10(param_2,param_3,lVar2,param_2,0);
  *(undefined8 *)(param_1 + 0x18) = 0xf;
  *(undefined8 *)(param_1 + 0x10) = 0;
  *param_1 = 0;
  FUN_18001a3f0(param_1,uVar1);
  return param_1;
}


/* 180089e10 string_combine_B */

undefined8 * FUN_180089e10(undefined8 *param_1,undefined8 *param_2,ulonglong param_3)

{
  code *pcVar1;
  undefined8 *puVar2;
  ulonglong uVar3;
  undefined8 *puVar4;
  ulonglong uVar5;
  
  if (param_2 != (undefined8 *)0x0) {
    uVar5 = param_1[3];
    puVar2 = param_1;
    if (0xf < uVar5) {
      puVar2 = (undefined8 *)*param_1;
    }
    if (puVar2 <= param_2) {
      puVar2 = param_1;
      if (0xf < uVar5) {
        puVar2 = (undefined8 *)*param_1;
      }
      if (param_2 < (undefined8 *)((longlong)puVar2 + param_1[2])) {
        puVar2 = param_1;
        if (0xf < uVar5) {
          puVar2 = (undefined8 *)*param_1;
        }
        uVar5 = (longlong)param_2 - (longlong)puVar2;
        if (uVar5 <= (ulonglong)param_1[2]) {
          uVar3 = param_1[2] - uVar5;
          if (uVar3 < param_3) {
            param_3 = uVar3;
          }
          if (param_3 < (ulonglong)~param_1[2]) {
            uVar3 = param_1[2] + param_3;
            if (param_3 != 0) {
              if (uVar3 == 0xffffffffffffffff) {
                __Xlength_error_std__YAXPEBD_Z(&UNK_1806d3bd0);
                pcVar1 = (code *)swi(3);
                puVar2 = (undefined8 *)(*pcVar1)();
                return puVar2;
              }
              if ((ulonglong)param_1[3] < uVar3) {
                FUN_180016340(param_1,uVar3);
                if (uVar3 == 0) {
                  return param_1;
                }
              }
              else if (uVar3 == 0) {
                param_1[2] = 0;
                if (0xf < (ulonglong)param_1[3]) {
                  *(undefined1 *)*param_1 = 0;
                  return param_1;
                }
                *(undefined1 *)param_1 = 0;
                return param_1;
              }
              puVar2 = param_1;
              if (0xf < (ulonglong)param_1[3]) {
                puVar2 = (undefined8 *)*param_1;
              }
              puVar4 = param_1;
              if (0xf < (ulonglong)param_1[3]) {
                puVar4 = (undefined8 *)*param_1;
              }
              if (param_3 != 0) {
                FUN_1805dc344((longlong)puVar4 + param_1[2],(longlong)puVar2 + uVar5,param_3);
              }
              param_1[2] = uVar3;
              puVar2 = param_1;
              if (0xf < (ulonglong)param_1[3]) {
                puVar2 = (undefined8 *)*param_1;
              }
              *(undefined1 *)((longlong)puVar2 + uVar3) = 0;
            }
            return param_1;
          }
          __Xlength_error_std__YAXPEBD_Z(&UNK_1806d3bd0);
          pcVar1 = (code *)swi(3);
          puVar2 = (undefined8 *)(*pcVar1)();
          return puVar2;
        }
        __Xout_of_range_std__YAXPEBD_Z(&UNK_1806d3bb8);
        pcVar1 = (code *)swi(3);
        puVar2 = (undefined8 *)(*pcVar1)();
        return puVar2;
      }
    }
  }
  if ((ulonglong)~param_1[2] <= param_3) {
    __Xlength_error_std__YAXPEBD_Z(&UNK_1806d3bd0);
    pcVar1 = (code *)swi(3);
    puVar2 = (undefined8 *)(*pcVar1)();
    return puVar2;
  }
  uVar5 = param_1[2] + param_3;
  if (param_3 != 0) {
    if (uVar5 == 0xffffffffffffffff) {
      __Xlength_error_std__YAXPEBD_Z(&UNK_1806d3bd0);
      pcVar1 = (code *)swi(3);
      puVar2 = (undefined8 *)(*pcVar1)();
      return puVar2;
    }
    if ((ulonglong)param_1[3] < uVar5) {
      FUN_180016340(param_1,uVar5);
      if (uVar5 == 0) {
        return param_1;
      }
    }
    else if (uVar5 == 0) {
      param_1[2] = 0;
      if (0xf < (ulonglong)param_1[3]) {
        *(undefined1 *)*param_1 = 0;
        return param_1;
      }
      *(undefined1 *)param_1 = 0;
      return param_1;
    }
    puVar2 = param_1;
    if (0xf < (ulonglong)param_1[3]) {
      puVar2 = (undefined8 *)*param_1;
    }
    if (param_3 != 0) {
      FUN_1805dc344((longlong)puVar2 + param_1[2],param_2,param_3);
    }
    param_1[2] = uVar5;
    puVar2 = param_1;
    if (0xf < (ulonglong)param_1[3]) {
      puVar2 = (undefined8 *)*param_1;
    }
    *(undefined1 *)((longlong)puVar2 + uVar5) = 0;
  }
  return param_1;
}


/* 1802ef740 ?SetCloudStorage@GameEngine@GAME@@QEAAX_N@Z */

void _SetCloudStorage_GameEngine_GAME__QEAAX_N_Z(longlong param_1,undefined1 param_2)

{
                    /* 0x2ef740  22690  ?SetCloudStorage@GameEngine@GAME@@QEAAX_N@Z */
  *(undefined1 *)(param_1 + 0x375ea) = param_2;
  return;
}


