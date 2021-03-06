﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using RimWorld;
using Verse;
using UnityEngine;

namespace CommunityCoreLibrary
{
    
    public class MainMenuDef : Def
    {

        #region XML Data

        public string               labelKey;
        public int                  order;
        public Type                 menuClass;
        public bool                 closeMainTab = false;

        #endregion

        [Unsaved]

        #region Instance Data

        public MainMenu             menuWorker;

        #endregion

        public override void PostLoad()
        {
            base.PostLoad();

            if(
                ( string.IsNullOrEmpty( labelKey ) )&&
                ( string.IsNullOrEmpty( label ) )
            )
            {
                CCL_Log.TraceMod(
                    this,
                    Verbosity.NonFatalErrors,
                    "Def must have a valid label or labelKey for translation"
                );
                label = defName;
            }

            if( !menuClass.IsSubclassOf( typeof( MainMenu ) ) )
            {
                CCL_Log.TraceMod(
                    this,
                    Verbosity.FatalErrors,
                    string.Format( "{0} is not a valid main menu class, does not inherit from MainMenu", menuClass.ToString() )
                );
            }
            else
            {
                menuWorker = (MainMenu) Activator.CreateInstance( menuClass );
                if( menuWorker == null )
                {
                    CCL_Log.TraceMod(
                        this,
                        Verbosity.FatalErrors,
                        string.Format( "Unable to create instance of {0}", menuClass.ToString() ),
                        "MainMenuDef" );
                }
            }

        }

    }

}
