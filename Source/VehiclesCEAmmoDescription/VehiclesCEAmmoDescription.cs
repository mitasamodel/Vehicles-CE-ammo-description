using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Vehicles;
using Verse;


namespace VehiclesCEAmmoDescription
{
	[StaticConstructorOnStartup]
	public static class VehiclesCEAmmoDescription
	{
		static readonly bool DEBUG = false;
		static readonly string logFile = @Environment.CurrentDirectory + @"\Mods\CEAmmoDescription_log.txt";

		static VehiclesCEAmmoDescription()
		{
			if (DEBUG) System.IO.File.WriteAllText(logFile, "CEAmmoDescription\n");	//create/rewrite file

			//Go through all "VehicleDef" in the game. Looking for Turrets and Upgrades (may contain turrets)
			foreach (VehicleDef vehicle in DefDatabase<VehicleDef>.AllDefsListForReading)
			{
				Log("Vehicle: " + vehicle.defName + "\n");

				//Turrets
				LinkTurrets(vehicle);

				//Upgrades
				LinkUpgrades(vehicle);

				Log("\n");
			}
		}

		/// <summary>
		/// <c>HyperlinksUpgrades</c> adds hyperlinks based on vehicle's upgrades
		/// </summary>
		private static void LinkUpgrades(VehicleDef vehicle)
		{
			//Vehicle has upgrades
			if (vehicle.GetCompProperties<CompProperties_UpgradeTree>() is CompProperties_UpgradeTree compUpgrades)
			{
				Log("Upgrade: " + compUpgrades.def + "\n");

				UpgradeTreeDef upgradeTree = compUpgrades.def as UpgradeTreeDef;

				//Each upgrade Tree can have multiple upgrade nodes
				foreach (UpgradeNode node in upgradeTree.nodes)
				{
					if (node.upgrades != null)		//yes, it can happen, that UpgradeTree exists, but there are no real upgrades inside... =/
					{
						//Each node can have several actual upgrades...
						foreach (Upgrade upgrade in node.upgrades)
						{
							//But we want turrets only
							if (upgrade is TurretUpgrade upgTurrets)
							{
								//Only upgrade, which adds turret (Upgrade can also remove turret)
								if (upgTurrets.turrets != null)
								{
									//Single upgrade can modify several turrets
									foreach (VehicleTurret turret in upgTurrets.turrets)
										LinkTurret(vehicle, turret.turretDef);
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
		private static void LinkTurrets(VehicleDef vehicle)
		{
			//Vehicle has turrets at all
			if (vehicle.GetCompProperties<CompProperties_VehicleTurrets>() is CompProperties_VehicleTurrets compTurrets)
			{
				//Several turrets can be attached to a single vehicle
				foreach (VehicleTurret turret in compTurrets.turrets)
					LinkTurret(vehicle, turret.turretDef);
			}
		}

		/// <summary>
		/// <c>LinkTurret</c> adds hyperlink for the turret
		/// </summary>
		private static void LinkTurret(VehicleDef vehicle, VehicleTurretDef turretDef)
		{
			//Check if this turret has ammoSet defined for CE: DefModExtension
			if (turretDef.HasModExtension<CETurretDataDefModExtension>())
			{
				Def ammoSet = turretDef.GetModExtension<CETurretDataDefModExtension>()._ammoSet;
				Log("Turret: " + turretDef + " - " + ammoSet + "\n");
				AddHyperlink(vehicle, ammoSet);
			}
		}

		private static void AddHyperlink(Def def, Def linkToAdd)
		{
			if (def == null || linkToAdd == null)
				return;
			if (def.descriptionHyperlinks == null)
				def.descriptionHyperlinks = new List<DefHyperlink>();
			if (def.descriptionHyperlinks.Any(tmp => tmp.def == linkToAdd))
				return;

			def.descriptionHyperlinks.Add(linkToAdd);
		}

		private static void Log(string line)
		{
			if (DEBUG) System.IO.File.AppendAllText(logFile, line);
		}
	}
}
