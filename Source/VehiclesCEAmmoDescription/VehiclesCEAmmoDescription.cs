using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using CombatExtended;
using CombatExtended.Compatibility;
using RimWorld;
using Vehicles;
using Verse;


namespace VehiclesCEAmmoDescription
{
	[StaticConstructorOnStartup]
	public static class VehiclesCEAmmoDescription
	{
		static VehiclesCEAmmoDescription()
		{
			bool DEBUG = false;

			//Debug log
			string logFile = @Environment.CurrentDirectory + @"\Mods\CEAmmoDescription_log.txt";
			StringBuilder log = new StringBuilder();

			//Turret dictionary: ["defName"] = ammoSet
			var turretsAmmoSet = TurretsAmmoSetDict();
			if (DEBUG) LogTurrets(log, turretsAmmoSet);

			//Upgrades dictionary: ["defName"] = UpgradeTreeDef
			var upgradesDict = UpgradesSetDict();
			if (DEBUG) LogUpgrades(log, upgradesDict);

			//Go through all "VehicleDef" in the game. Looking for Turrets and Upgrades (may contain turrets)
			foreach (VehicleDef vehicle in DefDatabase<VehicleDef>.AllDefsListForReading)
			{
				log.Append("Vehicle: " + vehicle.defName + "\n");

				//Turrets
				HyperlinksTurrets(vehicle, turretsAmmoSet, log);

				//Upgrades
				HyperlinksUpgrades(vehicle, upgradesDict, turretsAmmoSet);

				log.Append("\n");
			}

			if (DEBUG) System.IO.File.WriteAllText(logFile, log.ToString());
		}

		/// <summary>
		/// <c>HyperlinksUpgrades</c> adds hyperlinks based on vehicle's upgrades
		/// </summary>
		private static void HyperlinksUpgrades(VehicleDef vehicle, Dictionary<string, UpgradeTreeDef> upgradesDict, Dictionary<string, AmmoSetDef> turretsAmmoSet, StringBuilder log = null)
		{
			if (vehicle.GetCompProperties<CompProperties_UpgradeTree>() is CompProperties_UpgradeTree compUpgrades)
			{
				log?.Append("Upgrade: " + compUpgrades.def + "\n");

				UpgradeTreeDef upgradeTree = new UpgradeTreeDef();
				upgradeTree = upgradesDict[compUpgrades.def.ToString()];
				if (upgradeTree == null)
				{
					log?.Append("No upgrade found\n");
				}
				else
				{
					//Each upgrade Tree can have multiple upgrade nodes
					foreach (UpgradeNode node in upgradeTree.nodes)
					{
						//Each node can have several actual upgrades...
						foreach (Upgrade upgrade in node.upgrades)
						{
							//But we want turrets only
							if (upgrade is TurretUpgrade upgTurrets)
							{
								foreach (VehicleTurret turret in upgTurrets.turrets)
								{
									log?.Append("UPG Turret: " + turret.turretDef);

									//ammoSet exists for the turret
									if (turretsAmmoSet[turret.turretDef.ToString()] != null)
									{
										log?.AppendLine(" - " + turretsAmmoSet[turret.turretDef.ToString()]);

										AddHyperlinkAmmoSet(vehicle, turretsAmmoSet[turret.turretDef.ToString()], log);
									}
								}
							}
						}
					}
				}
			}
		}

		/// <summary>
		/// <c>HyperlinksTurrets</c> adds hyperlinks for vehicle's turrets
		/// </summary>
		private static void HyperlinksTurrets(VehicleDef vehicle, Dictionary<string, AmmoSetDef> turretsAmmoSet, StringBuilder log = null)
		{
			if (vehicle.GetCompProperties<CompProperties_VehicleTurrets>() is CompProperties_VehicleTurrets compTurrets)
			{
				//Several turrets can be attached to a single vehicle
				foreach (VehicleTurret turret in compTurrets.turrets)
				{
					log?.Append("Turret: " + turret.turretDef);

					//ammoSet exists for the turret
					if (turretsAmmoSet[turret.turretDef.ToString()] != null)
					{
						log?.AppendLine(" - " + turretsAmmoSet[turret.turretDef.ToString()]);

						AddHyperlinkAmmoSet(vehicle, turretsAmmoSet[turret.turretDef.ToString()]);
					}
				}
			}
		}

		/// <summary>
		/// <c>AddHyperlinkAmmoSet</c> checks and adds new Hyperlink
		/// </summary>
		private static void AddHyperlinkAmmoSet(VehicleDef vehicle, AmmoSetDef ammoSet, StringBuilder log = null)
		{
			if (ammoSet == null)
				return;

			AddHyperlink(vehicle, ammoSet, log);
		}

		private static void AddHyperlink(Def def, Def linkToAdd, StringBuilder log = null)
		{
			if (def == null) return;
			if (def.descriptionHyperlinks == null)
				def.descriptionHyperlinks = new List<DefHyperlink>();
			foreach (DefHyperlink item in def.descriptionHyperlinks)
			{
				if (item.def == linkToAdd)
				{
					log?.AppendLine("Link duplicate: " + item.def);
					return;
				}
			}
			def.descriptionHyperlinks.Add(linkToAdd);
		}

		/// <summary>
		/// <c>MakeTurretList</c> goes through all turrets and creates dictionary ["defName"] = ammoSet
		/// </summary>
		private static Dictionary<string, AmmoSetDef> TurretsAmmoSetDict()
		{
			var dict = new Dictionary<string, AmmoSetDef>();

			foreach (VehicleTurretDef turret in DefDatabase<VehicleTurretDef>.AllDefsListForReading)
			{
				//Check if this turret has ammoSet defined for CE: DefModExtension
				if (turret.HasModExtension<CETurretDataDefModExtension>())
				{
					//well, it is not O(n^2), but we go through this array (list) for every turret. It is done only once... so...
					foreach (AmmoSetDef ammoSet in DefDatabase<AmmoSetDef>.AllDefsListForReading)
					{
						if (ammoSet.defName == turret.GetModExtension<CETurretDataDefModExtension>().ammoSet)
							dict[turret.defName] = ammoSet;
					}
				}
				else
					dict[turret.defName] = null;
			}
			return dict;
		}

		/// <summary>
		/// <c>UpgradesSetDict</c> goes through all upgrades and creates dictionary ["defName"] = UpgradeTreeDef
		/// </summary>
		private static Dictionary<string, UpgradeTreeDef> UpgradesSetDict()
		{
			Dictionary<string, UpgradeTreeDef> dict = new Dictionary<string, UpgradeTreeDef>();

			foreach (UpgradeTreeDef upgradeDef in DefDatabase<UpgradeTreeDef>.AllDefsListForReading)
			{
				dict[upgradeDef.defName] = upgradeDef;
			}

			return dict;
		}

		/// <summary>
		/// Method <c>LogTurrets</c> display the Turret dictionary
		/// </summary>
		private static void LogTurrets(StringBuilder log, Dictionary<string, AmmoSetDef> turrets)
		{
			foreach (var turret in turrets)
			{
				if (turret.Value != null)
					log?.Append("Turret: " + turret.Key + " - " + turret.Value.defName + "\n");
				else
					log?.Append("Turret: " + turret.Key + " - \n");
			}

			log?.Append("\n");
		}

		/// <summary>
		/// <c>LogTurrets</c> displays the upgrade dictionary
		/// </summary>
		private static void LogUpgrades(StringBuilder log, Dictionary<string, UpgradeTreeDef> upgrades)
		{
			foreach (var upgrade in upgrades)
			{
				log.Append("Upgrade: " + upgrade.Key + " - " + upgrade.Value.defName + "\n");
			}
			log.Append("\n");
		}
	}
}
