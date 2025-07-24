using CombatExtended;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using Vehicles;
using Verse;


namespace VehiclesCEAmmoDescription
{
	[StaticConstructorOnStartup]
	public static class VehiclesCEAmmoDescription
	{
		static VehiclesCEAmmoDescription()
		{
#if DEBUG
			Verse.Log.Warning("[VehiclesCEAmmoDescription] Start");
#endif

			//Go through all "VehicleDef" in the game. Looking for Turrets and Upgrades (may contain turrets)
			foreach (VehicleDef vehicle in DefDatabase<VehicleDef>.AllDefsListForReading)
			{
				Log($"Vehicle: {vehicle.defName}");

				//Turrets
				LinkTurrets(vehicle);

				//Upgrades
				LinkUpgrades(vehicle);

				Log("");
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
				Log($"Upgrade: {compUpgrades.def}");

				UpgradeTreeDef upgradeTree = compUpgrades.def as UpgradeTreeDef;

				//Each upgrade Tree can have multiple upgrade nodes
				foreach (UpgradeNode node in upgradeTree.nodes)
				{
					if (node.upgrades != null)      //yes, it can happen, that UpgradeTree exists, but there are no real upgrades inside... =/
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
										LinkTurret(vehicle, turret.GetTurretDef());
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
				{
					Log($"Turret: {turret?.Name}");

					if (turret != null)
						LinkTurret(vehicle, turret.GetTurretDef());
				}
			}
		}

		/// <summary>
		/// <c>LinkTurret</c> adds hyperlink for the turret
		/// </summary>
		private static void LinkTurret(VehicleDef vehicle, VehicleTurretDef turretDef)
		{
			if (turretDef == null)
			{
				Verse.Log.Error($"[VehiclesCEAmmoDescription] Vehicle {vehicle} has NULL turret def");
				return;
			}
			Log($"Turret: {turretDef.defName}");

			//Check if this turret has ammoSet defined for CE: DefModExtension
			if (turretDef.HasModExtension<CETurretDataDefModExtension>())
			{
				//Try to get Class directly
				Def ammoSet = turretDef.GetModExtension<CETurretDataDefModExtension>()._ammoSet ??
					//Or look up the Class by the ammoSet name
					VehicleTurret.LookupAmmosetCE(turretDef.GetModExtension<CETurretDataDefModExtension>().ammoSet);

				if (ammoSet != null)
				{
					Log($"AmmoSet: {ammoSet.defName}");

					AddHyperlink(vehicle, ammoSet);
				}
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

		public static void Log(string message)
		{
#if DEBUG
			Verse.Log.Message(message);
#endif
		}
	}

	/// <summary>
	/// VF changed the field `turretDef` => `def`. This is the look up of the alias in new version
	/// This supports both old (1.5) and new (1.6) version and potentially further versions
	/// </summary>
	public static class VehiclesCEAmmoDescription_Compat
	{
		//just in case the visibility will be changed
		private static readonly BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
		//Check 1.5 and 1.6 fields. Then try to check 'LoadAlias' attribute
		private static readonly FieldInfo turretDefField = typeof(VehicleTurret).GetField("def", flags) ??
			typeof(VehicleTurret).GetField("turretDef", flags) ??
			typeof(VehicleTurret)
			.GetFields()
			.FirstOrDefault(f => f.GetCustomAttribute<LoadAliasAttribute>()?.alias == "turretDef");

		public static VehicleTurretDef GetTurretDef(this VehicleTurret turret)
		{
			if (turretDefField == null)
				Verse.Log.Error("[VehiclesCEAmmoDescription_Compat] Could not find VehicleTurretDef field.");

			//if (turretDefField?.GetValue(turret) is VehicleTurretDef def)
			//	return def;
			//else
			//	return null;
			return turretDefField?.GetValue(turret) as VehicleTurretDef;
		}
	}
}
