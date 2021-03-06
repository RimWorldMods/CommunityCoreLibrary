﻿using RimWorld;
using Verse;
using UnityEngine;

namespace CommunityCoreLibrary
{

	public class MainMenu_HelpMenu : MainMenu
	{

		public override bool RenderNow( bool anyWorldFiles, bool anyMapFiles )
		{
			return ( Game.Mode == GameMode.Entry );
		}

		public override void ClickAction()
		{
			Find.WindowStack.Add( (Window)new MainTabWindow_ModHelp() );
		}

	}

}
