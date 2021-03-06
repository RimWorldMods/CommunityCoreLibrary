﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using RimWorld;
using Verse;
using UnityEngine;

namespace CommunityCoreLibrary
{

    public static class ThingDef_Extensions
    {

        // Dummy for functions needing a ref list
        public static List<Def>             nullDefs = null;

        #region Recipe Cache

        public static void                  RecacheRecipes( this ThingDef thingDef, bool validateBills )
        {
            typeof( ThingDef ).GetField( "allRecipesCached", BindingFlags.Instance | BindingFlags.NonPublic ).SetValue( thingDef, null );

            if(
                ( !validateBills )||
                ( Game.Mode != GameMode.MapPlaying )
            )
            {
                return;
            }

            // Get the recached recipes
            var recipes = thingDef.AllRecipes;

            // Remove bill on any table of this def using invalid recipes
            var buildings = Find.ListerBuildings.AllBuildingsColonistOfDef( thingDef );
            foreach( var building in buildings )
            {
                var BillGiver = building as IBillGiver;
                if( BillGiver != null )
                {
                    for( int i = 0; i < BillGiver.BillStack.Count; ++ i )
                    {
                        var bill = BillGiver.BillStack[ i ];
                        if( !recipes.Exists( r => bill.recipe == r ) )
                        {
                            BillGiver.BillStack.Delete( bill );
                            continue;
                        }
                    }
                }
            }

        }

        #endregion

        #region Availability

        public static bool                  IsIngestible( this ThingDef thingDef )
        {
            if(
                (
                    ( thingDef.thingClass == typeof( Meal ) )||
                    ( thingDef.thingClass.IsSubclassOf( typeof( Meal ) ) )
                )&&
                ( thingDef.ingestible != null )
            )
            {
                return true;
            }
            return false;
        }

        public static bool                  IsAlcohol( this ThingDef thingDef )
        {
            if(
                ( thingDef.IsIngestible() )&&
                ( !thingDef.ingestible.hediffGivers.NullOrEmpty() )&&
                ( thingDef.ingestible.hediffGivers.Exists( d => d.hediffDef == HediffDefOf.Alcohol ) )
            )
            {
                return true;
            }
            return false;
        }

        public static bool                  IsImplant( this ThingDef thingDef )
        {
            // Return true if a recipe exist implanting this thing def
            return
                DefDatabase< RecipeDef >.AllDefsListForReading.Exists( r => (
                    ( r.addsHediff != null )&&
                    ( r.IsIngredient( thingDef ) )
                ) );
        }

        public static RecipeDef             GetImplantRecipeDef( this ThingDef thingDef )
        {
            // Get recipe for implant
            return
                DefDatabase< RecipeDef >.AllDefsListForReading.Find( r => (
                    ( r.addsHediff != null )&&
                    ( r.IsIngredient( thingDef ) )
                ) );
        }

        public static HediffDef             GetImplantHediffDef( this ThingDef thingDef )
        {
            // Get hediff for implant
            var recipeDef = thingDef.GetImplantRecipeDef();
            return recipeDef != null
                ? recipeDef.addsHediff
                    : null;
        }

        public static bool                  EverHasRecipes( this ThingDef thingDef )
        {
            return (
                ( !thingDef.GetRecipesCurrent().NullOrEmpty() )||
                ( !thingDef.GetRecipesUnlocked( ref nullDefs ).NullOrEmpty() )||
                ( !thingDef.GetRecipesLocked( ref nullDefs ).NullOrEmpty() )
            );
        }

        public static bool                  EverHasRecipe( this ThingDef thingDef, RecipeDef recipeDef )
        {
            return (
                ( thingDef.GetRecipesCurrent().Contains( recipeDef ) )||
                ( thingDef.GetRecipesUnlocked( ref nullDefs ).Contains( recipeDef ) )||
                ( thingDef.GetRecipesLocked( ref nullDefs ).Contains( recipeDef ) )
            );
        }

        public static JoyGiverDef           GetJoyDefUsing( this ThingDef thingDef )
        {
            return DefDatabase<JoyGiverDef>.AllDefs.FirstOrDefault( def => def.thingDef == thingDef );
        }

        #endregion

        #region Lists of affected data

        public static List< RecipeDef >     GetRecipesUnlocked( this ThingDef thingDef, ref List< Def > researchDefs )
        {
#if DEBUG
            CCL_Log.TraceMod(
                thingDef,
                Verbosity.Stack,
                "GetRecipesUnlocked()"
            );
#endif
            // Things it is unlocked on with research
            var recipeDefs = new List<RecipeDef>();
            if( researchDefs != null )
            {
                researchDefs.Clear();
            }

            // Look at recipes
            var recipes = DefDatabase< RecipeDef >.AllDefsListForReading.Where( r => (
                ( r.researchPrerequisite != null )&&
                ( r.recipeUsers != null )&&
                ( r.recipeUsers.Contains( thingDef ) )&&
                ( !r.IsLockedOut() )
            ) ).ToList();

            // Look in advanced research too
            var advancedResearch = Controller.Data.AdvancedResearchDefs.Where( a => (
                ( a.IsRecipeToggle )&&
                ( !a.HideDefs )&&
                ( a.thingDefs.Contains( thingDef ) )
            ) ).ToList();

            // Aggregate advanced research
            foreach( var a in advancedResearch )
            {
                recipeDefs.AddRange( a.recipeDefs );
                if( researchDefs != null )
                {
                    if( a.researchDefs.Count == 1 )
                    {
                        // If it's a single research project, add that
                        researchDefs.Add( a.researchDefs[ 0 ] );
                    }
                    else
                    {
                        // Add the advanced project instead
                        researchDefs.Add( a );
                    }
                }
            }
            return recipeDefs;
        }

        public static List< RecipeDef >     GetRecipesLocked( this ThingDef thingDef, ref List< Def > researchDefs )
        {
#if DEBUG
            CCL_Log.TraceMod(
                thingDef,
                Verbosity.Stack,
                "GetRecipesLocked()"
            );
#endif
            // Things it is locked on with research
            var recipeDefs = new List<RecipeDef>();
            if( researchDefs != null )
            {
                researchDefs.Clear();
            }

            // Look in advanced research
            var advancedResearch = Controller.Data.AdvancedResearchDefs.Where( a => (
                ( a.IsRecipeToggle )&&
                ( a.HideDefs )&&
                ( a.thingDefs.Contains( thingDef ) )
            ) ).ToList();

            // Aggregate advanced research
            foreach( var a in advancedResearch )
            {
                recipeDefs.AddRange( a.recipeDefs );

                if( researchDefs != null )
                {
                    if( a.researchDefs.Count == 1 )
                    {
                        // If it's a single research project, add that
                        researchDefs.Add( a.researchDefs[ 0 ] );
                    }
                    else if( a.ResearchConsolidator != null )
                    {
                        // Add the advanced project instead
                        researchDefs.Add( a.ResearchConsolidator );
                    }
                }
            }

            return recipeDefs;
        }

        public static List< RecipeDef >     GetRecipesCurrent( this ThingDef thingDef )
        {
#if DEBUG
            CCL_Log.TraceMod(
                thingDef,
                Verbosity.Stack,
                "GetRecipesCurrent()"
            );
#endif
            return thingDef.AllRecipes;
        }

        public static List< RecipeDef >     GetRecipesAll( this ThingDef thingDef )
        {
#if DEBUG
            CCL_Log.TraceMod(
                thingDef,
                Verbosity.Stack,
                "GetRecipesAll()"
            );
#endif
            // Things it is locked on with research
            var recipeDefs = new List<RecipeDef>();

            recipeDefs.AddRange( thingDef.GetRecipesCurrent() );
            recipeDefs.AddRange( thingDef.GetRecipesUnlocked( ref nullDefs ) );
            recipeDefs.AddRange( thingDef.GetRecipesLocked( ref nullDefs ) );

            return recipeDefs;
        }

        #endregion

        #region Comp Properties

        // Get CompProperties by CompProperties class
        // Similar to GetCompProperties which gets CompProperties by compClass
        public static CompProperties        GetCompProperty( this ThingDef thingDef, Type find )
        {
            foreach( var comp in thingDef.comps )
            {
                if( comp.GetType() == find )
                {
                    return comp;
                }
            }
            return null;
        }

        public static CommunityCoreLibrary.CompProperties_ColoredLight CompProperties_ColoredLight ( this ThingDef thingDef )
        {
            return thingDef.GetCompProperties( typeof( CommunityCoreLibrary.CompColoredLight ) ) as CommunityCoreLibrary.CompProperties_ColoredLight;
        }

        public static CommunityCoreLibrary.CompProperties_LowIdleDraw CompProperties_LowIdleDraw ( this ThingDef thingDef )
        {
            return thingDef.GetCompProperties( typeof( CommunityCoreLibrary.CompPowerLowIdleDraw ) ) as CommunityCoreLibrary.CompProperties_LowIdleDraw;
        }

        public static Verse.CompProperties_Rottable CompProperties_Rottable ( this ThingDef thingDef )
        {
            return thingDef.GetCompProperties( typeof( RimWorld.CompRottable ) ) as Verse.CompProperties_Rottable;
        }

        #endregion

        #region Joy Participant Cells (Watch Buildings)

        public static List< IntVec3 >       GetParticipantCells( this ThingDef thingDef, IntVec3 position, Rot4 rotation, bool getBlocked = false )
        {
            var joyGiverDef = thingDef.GetJoyDefUsing();
            if( joyGiverDef == null )
            {
                // No joy giver which uses this def
                return null;
            }
            if(
                ( joyGiverDef.standDistanceRange.min < 1 )||
                ( joyGiverDef.standDistanceRange.max < 1 )
            )
            {
                // no range?
                return null;
            }
            var returnCells = new List<IntVec3>();
            var allowedDirections = new List<int>();
            if( thingDef.rotatable )
            {
                allowedDirections.Add( rotation.AsInt );
            }
            else
            {
                for( int i = 0; i < 4; ++i )
                {
                    allowedDirections.Add( i );
                }
            }
            for( int index1 = 0; index1 < allowedDirections.Count; ++index1 )
            {
                CellRect cellRect;
                if( new Rot4( allowedDirections[ index1 ] ).IsHorizontal )
                {
                    int a = position.x + GenAdj.CardinalDirections[ allowedDirections[ index1 ] ].x * joyGiverDef.standDistanceRange.min;
                    int b = position.x + GenAdj.CardinalDirections[ allowedDirections[ index1 ] ].x * joyGiverDef.standDistanceRange.max;
                    int num = position.z + 1;
                    int minZ = position.z - 1;
                    cellRect = new CellRect(
                        Mathf.Min( a, b ),
                        minZ,
                        Mathf.Abs( a - b ) + 1,
                        num - minZ + 1 );
                }
                else
                {
                    int a = position.z + GenAdj.CardinalDirections[ allowedDirections[ index1 ] ].z * joyGiverDef.standDistanceRange.min;
                    int b = position.z + GenAdj.CardinalDirections[ allowedDirections[ index1 ] ].z * joyGiverDef.standDistanceRange.max;
                    int num = position.x + 1;
                    int minX = position.x - 1;
                    cellRect = new CellRect(
                        minX,
                        Mathf.Min( a, b ),
                        num - minX + 1,
                        Mathf.Abs( a - b ) + 1 );
                }
                IntVec3 center = cellRect.Center;
                int num1 = cellRect.Area * 4;
                for( int index2 = 0; index2 < num1; ++index2 )
                {
                    IntVec3 intVec3 = center + GenRadial.RadialPattern[ index2 ];
                    if( cellRect.Contains( intVec3 ) )
                    {
                        if(
                            (
                                ( getBlocked )&&
                                (
                                    ( !GenGrid.Standable( intVec3 ) )||
                                    ( !GenSight.LineOfSight( intVec3, position, false ) )
                                )
                            )||
                            (
                                ( !getBlocked )&&
                                ( GenGrid.Standable( intVec3 ) )&&
                                ( GenSight.LineOfSight( intVec3, position, false ) )
                            )
                        )
                        {
                            returnCells.Add( intVec3 );
                        }
                    }
                }
            }
            return returnCells;
        }

        #endregion

    }

}
