When we have a mainmenu and Points to spend:

GameDataManager.PrestigePoints carries the number of Points we have

Please go into GameDataManager and set the MAIN_MENU_SCENE to the correct scene name (when we have one)

.ResetAll resets all Points AND EVERYTHING ELSE stored in the save file

SaveData only has Prestige Points rn, ist a list, just add every other type of data you want saved in there.

Use something like
´´´
if (GDM.TrySpendPrestigePoints(cost))
{
	Do Thing
	Set bool CoolSkill to 1 in SaveData
}
else
{
	Show text "Not Enough Points womp womp"
}

´´´
in the Main menu script (or add it to the relevant shop/Dojo) to handle buying Upgrades. Points are deducted automatically in the GDM.