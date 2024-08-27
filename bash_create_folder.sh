#!/bin/bash

read -p "Mod name: " modName
mkdir -p "ModPatches/$modName/Patches/$modName"
cp "ModPatches/_template/Patches/_template/Vehicles.xml" "ModPatches/$modName/Patches/$modName"
read -n 1 -s -r -p "Press any key to continue"
