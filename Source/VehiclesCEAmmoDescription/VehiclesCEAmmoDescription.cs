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

			//Go through all "VehicleDef" in the game. Looking for Turrets and Upgrades (may contain turrets)
			foreach (VehicleDef vehicle in DefDatabase<VehicleDef>.AllDefsListForReading)
			{
				log.Append("Vehicle: " + vehicle.defName + "\n");

				//Turrets
				LinkTurrets(vehicle, log);

				//Upgrades
				LinkUpgrades(vehicle, log);

				log.Append("\n");
			}

			if (DEBUG) System.IO.File.WriteAllText(logFile, log.ToString());
		}

		/// <summary>
		/// <c>HyperlinksUpgrades</c> adds hyperlinks based on vehicle's upgrades
		/// </summary>
		private static void LinkUpgrades(VehicleDef vehicle, StringBuilder log = null)
		{
			//Vehicle has upgrades
			if (vehicle.GetCompProperties<CompProperties_UpgradeTree>() is CompProperties_UpgradeTree compUpgrades)
			{
				log?.AppendLine("Upgrade: " + compUpgrades.def);

				UpgradeTreeDef upgradeTree = compUpgrades.def as UpgradeTreeDef;

				//Each upgrade Tree can have multiple upgrade nodes
				foreach (UpgradeNode node in upgradeTree.nodes)
				{
					//Each node can have several actual upgrades...
					foreach (Upgrade upgrade in node.upgrades)
					{
						//But we want turrets only
						if (upgrade is TurretUpgrade upgTurrets)
						{
							//Single upgrade can modify several turrets
							foreach (VehicleTurret turret in upgTurrets.turrets)
								LinkTurret(vehicle, turret.turretDef, log);
						}
					}
				}
			}
		}

		/// <summary>
		/// <c>HyperlinksTurrets</c> adds hyperlinks for vehicle's turrets
		/// </summary>
		private static void LinkTurrets(VehicleDef vehicle, StringBuilder log = null)
		{
			//Vehicle has turrets at all
			if (vehicle.GetCompProperties<CompProperties_VehicleTurrets>() is CompProperties_VehicleTurrets compTurrets)
			{
				//Several turrets can be attached to a single vehicle
				foreach (VehicleTurret turret in compTurrets.turrets)
					LinkTurret(vehicle, turret.turretDef, log);
			}
		}

		/// <summary>
		/// <c>LinkTurret</c> adds hyperlink for the turret
		/// </summary>
		private static void LinkTurret(VehicleDef vehicle, VehicleTurretDef turretDef, StringBuilder log = null)
		{
			//Check if this turret has ammoSet defined for CE: DefModExtension
			if (turretDef.HasModExtension<CETurretDataDefModExtension>())
			{
				Def ammoSet = turretDef.GetModExtension<CETurretDataDefModExtension>()._ammoSet;
				log?.AppendLine("Turret: " + turretDef + " - " + ammoSet);
				AddHyperlink(vehicle, ammoSet);
			}
		}

		private static void AddHyperlink(Def def, Def linkToAdd, StringBuilder log = null)
		{
			if (def == null || linkToAdd == null)
				return;
			if (def.descriptionHyperlinks == null)
				def.descriptionHyperlinks = new List<DefHyperlink>();
			if (def.descriptionHyperlinks.Any(tmp => tmp.def == linkToAdd))
				return;

			def.descriptionHyperlinks.Add(linkToAdd);
		}
	}
}
