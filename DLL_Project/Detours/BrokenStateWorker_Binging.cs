﻿using System;
using System.Linq;
using System.Collections.Generic;

using RimWorld;
using Verse;
using Verse.AI;

namespace CommunityCoreLibrary.Detour
{

    internal static class _BrokenStateWorker_Binging
    {
        
        internal static bool _StateCanOccur( this BrokenStateWorker_Binging obj, Pawn pawn )
        {
            if(
                ( pawn.Faction != Faction.OfColony )||
                ( PawnUtility.GetPosture( pawn ) != PawnPosture.Standing )
            )
            {
                return false;
            }
            return
                Find.ListerThings.AllThings.Any( t => (
                    ( t.def.ingestible != null )&&
                    ( t.def.ingestible.hediffGivers != null )&&
                    ( t.def.ingestible.hediffGivers.Any( h => (
                        ( h.hediffDef == HediffDefOf.Alcohol )
                    ) ) )
                ) );
        }
    }

}
