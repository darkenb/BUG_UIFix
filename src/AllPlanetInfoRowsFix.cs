using System;
using System.Reflection;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;

namespace BUG_UIFix
{
    // DSPAllPlanetInfo's "Interstellar Logistics" rows (InfoCreater.StationInfo), from our local 1.1.15:
    // - a station with more slots than fit on a page continues on the next page from its next slot (it was skipped, and a
    //   station larger than a page made empty pages forever);
    // - rows never pass the window's row objects, the page number stays inside the mod's page table, and the previous
    //   button does nothing on the first page;
    // - a slot whose item no longer exists (removed mod) is skipped instead of throwing.
    public static class AllPlanetInfoRowsFix
    {
        const int Pages = 100;
        static FieldInfo lineMax, pageNo, lastStationNo, startStationNo;
        static GameObject[] names, icons, counts, logics, lines;
        static FieldInfo infoWindow, nextButton, previousButton, posY;
        static readonly int[] startSlotNo = new int[Pages];
        static int lastSlotNo;

        public static string Apply(Harmony harmony)
        {
            if (!Plugin.Loaded(AllPlanetInfoFix.Guid)) return "not loaded, nothing to do";
            Type creater = AccessTools.TypeByName("DSPAllPlanetInfo.InfoCreater").ThrowIfMissing("DSPAllPlanetInfo.InfoCreater");
            if (AccessTools.Field(creater, "startSlotNo") != null) return "already contains this fix, skipped";
            Type ui = AccessTools.TypeByName("DSPAllPlanetInfo.UI").ThrowIfMissing("DSPAllPlanetInfo.UI");
            FieldInfo F(Type t, string n) => AccessTools.Field(t, n).ThrowIfMissing(t.Name + "." + n);
            lineMax = F(creater, "lineMax"); pageNo = F(creater, "pageNo"); lastStationNo = F(creater, "lastStationNo"); startStationNo = F(creater, "startStationNo");
            infoWindow = F(ui, "infoWindow"); nextButton = F(ui, "nextButton"); previousButton = F(ui, "previousButton"); posY = F(ui, "posY");
            // The row arrays are created once in the mod's static fields; the GameObjects in them come later.
            names = (GameObject[])F(ui, "ItemName").GetValue(null); icons = (GameObject[])F(ui, "ItemIcon").GetValue(null);
            counts = (GameObject[])F(ui, "ItemCount").GetValue(null); logics = (GameObject[])F(ui, "ItemLogic").GetValue(null);
            lines = (GameObject[])F(ui, "Line").GetValue(null);
            harmony.Patch(AccessTools.Method(creater, "StationInfo").ThrowIfMissing("InfoCreater.StationInfo"), prefix: new HarmonyMethod(typeof(AllPlanetInfoRowsFix), nameof(StationInfo)));
            harmony.Patch(AccessTools.Method(ui, "OnClickPreviousButton").ThrowIfMissing("UI.OnClickPreviousButton"), prefix: new HarmonyMethod(typeof(AllPlanetInfoRowsFix), nameof(Previous)));
            harmony.Patch(AccessTools.Method(ui, "OnClickNextButton").ThrowIfMissing("UI.OnClickNextButton"), prefix: new HarmonyMethod(typeof(AllPlanetInfoRowsFix), nameof(Next)));
            return "logistics rows continue across pages";
        }

        static int Page { get => (int)pageNo.GetValue(null); set => pageNo.SetValue(null, value); }
        static int[] StartStation => (int[])startStationNo.GetValue(null);

        static bool Previous()
        {
            if (Page > 0) { Page--; UIRoot.instance.uiGame.planetDetail.RefreshDynamicProperties(); }
            return false;
        }

        static bool Next()
        {
            if (Page < Math.Min(StartStation.Length, Pages) - 1)
            {
                Page++;
                StartStation[Page] = (int)lastStationNo.GetValue(null);
                startSlotNo[Page] = lastSlotNo;
                UIRoot.instance.uiGame.planetDetail.RefreshDynamicProperties();
            }
            return false;
        }

        static void Row(int row, bool on) { names[row].SetActive(on); icons[row].SetActive(on); counts[row].SetActive(on); logics[row].SetActive(on); }

        // InfoCreater.StationInfo is static with a parameter named __instance: Harmony would fill that name with null, so __0.
        static bool StationInfo(UIPlanetDetail __0)
        {
            UIPlanetDetail __instance = __0;
            if (__instance.planet == null || !__instance.planet.scanned || infoWindow.GetValue(null) == null) return false;
            var next = (GameObject)nextButton.GetValue(null);
            int max = Math.Max(2, Math.Min((int)lineMax.GetValue(null), Math.Min(names.Length - 1, lines.Length - 1)));
            lineMax.SetValue(null, max);
            int page = Page = Math.Max(0, Math.Min(Page, Math.Min(StartStation.Length, Pages) - 1));
            float top = (float)posY.GetValue(null);
            int line = 0, row = 0;
            next.SetActive(false);
            ((GameObject)previousButton.GetValue(null)).SetActive(page > 0);
            PlanetFactory factory = UIRoot.instance.uiGame.planetDetail.planet.factory;
            if (factory == null)
            {
                for (int m = 1; m < names.Length; m++) Row(m, false);
                for (int n = 1; n < lines.Length; n++) lines[n].SetActive(false);
                names[1].SetActive(true);
                names[1].GetComponent<Text>().text = "No Interstellar Logistics Station".Translate();
                return false;
            }
            if (factory.transport.stationCursor > 0)
            {
                for (int i = StartStation[page]; i < factory.transport.stationCursor; i++)
                {
                    StationComponent station = factory.transport.stationPool[i];
                    if (station == null || !station.isStellar) continue;
                    if (row > 0 && row + station.storage.Length + 1 > max)
                    {
                        next.SetActive(true);
                        lastStationNo.SetValue(null, i);
                        lastSlotNo = 0;
                        break;
                    }
                    lines[line].transform.localPosition = new Vector3(-125f, top - 10f - row * 20, 0f);
                    lines[line].SetActive(true);
                    Row(row, false);
                    line++;
                    row++;
                    for (int j = i == StartStation[page] ? startSlotNo[page] : 0; j < station.storage.Length; j++)
                    {
                        if (station.storage[j].itemId != 0)
                        {
                            ItemProto item = LDB.items.Select(station.storage[j].itemId);
                            if (item == null) continue;
                            names[row].GetComponent<Text>().text = item.name;
                            icons[row].GetComponent<Image>().sprite = item.iconSprite;
                            counts[row].GetComponent<Text>().text = $"{station.storage[j].count:#,0}";
                            Text logic = logics[row].GetComponent<Text>();
                            if (station.storage[j].remoteLogic == ELogisticStorage.Demand) { logic.text = "需求".Translate(); logic.color = new Color(0.88f, 0.55f, 0.36f, 0.5f); }
                            else if (station.storage[j].remoteLogic == ELogisticStorage.Supply) { logic.text = "供应".Translate(); logic.color = new Color(0.24f, 0.55f, 0.65f, 0.5f); }
                            else { logic.text = "仓储".Translate(); logic.color = new Color(1f, 1f, 1f, 0.3f); }
                            Row(row, true);
                            row++;
                        }
                        if (row == max)
                        {
                            bool more = j + 1 < station.storage.Length;
                            lastStationNo.SetValue(null, more ? i : i + 1);
                            lastSlotNo = more ? j + 1 : 0;
                            next.SetActive((more ? i : i + 1) < factory.transport.stationCursor);
                            break;
                        }
                    }
                    if (row >= max) break;
                }
            }
            else
            {
                lines[0].transform.localPosition = new Vector3(-125f, top - 10f - row * 20, 0f);
                lines[0].SetActive(true);
                line++;
                row++;
                names[1].GetComponent<Text>().text = "No Interstellar Logistics Station".Translate();
                icons[1].SetActive(false); counts[1].SetActive(false); logics[1].SetActive(false);
                row++;
            }
            lines[line].transform.localPosition = new Vector3(-125f, top - 10f - row * 20, 0f);
            lines[line].SetActive(true);
            line++;
            for (int k = row; k < names.Length; k++) Row(k, false);
            for (int l = line; l < lines.Length; l++) lines[l].SetActive(false);
            return false;
        }
    }
}
