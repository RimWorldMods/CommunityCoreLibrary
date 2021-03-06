﻿using CommunityCoreLibrary.ResearchTree;
using System;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace CommunityCoreLibrary
{
    public static class ResearchProjectDef_Extensions
    {
        #region Static Data

        private static Dictionary<ResearchProjectDef, bool> isLockedOut = new Dictionary<ResearchProjectDef, bool>();

        private static Dictionary<Def, List<Pair<Def, string>>> _unlocksCache = new Dictionary<Def, List<Pair<Def, string>>>();

        #endregion Static Data

        #region Availability

        public static bool IsLockedOut( this ResearchProjectDef researchProjectDef, ResearchProjectDef initialDef = null )
        {
            bool rVal = false;
#if DEBUG
            CCL_Log.TraceMod(
                researchProjectDef,
                Verbosity.Stack,
                "IsLockedOut()"
            );
#endif
            if ( !isLockedOut.TryGetValue( researchProjectDef, out rVal ) )
            {
                if ( initialDef == null )
                {
                    // Stop cyclical recursion before it starts
                    initialDef = researchProjectDef;
                }
                // Check for possible unlock
                if ( !researchProjectDef.prerequisites.NullOrEmpty() )
                {
                    // Check each prerequisite
                    foreach ( var p in researchProjectDef.prerequisites )
                    {
                        if (
                            ( p.defName == initialDef.defName )||
                            ( p.IsLockedOut( initialDef ) )
                        )
                        {
                            // Cyclical-prerequisite or parent locked means potential lock-out

                            // Check for possible unlock
                            if ( !Controller.Data.AdvancedResearchDefs.Any( a => (
                                 ( a.IsResearchToggle )&&
                                 ( !a.HideDefs )&&
                                 ( a.effectedResearchDefs.Contains( researchProjectDef ) )
                             ) ) )
                            {
                                // No unlockers, always locked out
                                rVal = true;
                            }
                        }
                    }
                }
                isLockedOut.Add( researchProjectDef, rVal );
            }
            return rVal;
        }

        public static bool HasResearchRequirement( this ResearchProjectDef researchProjectDef )
        {
#if DEBUG
            CCL_Log.TraceMod(
                researchProjectDef,
                Verbosity.Stack,
                "HasResearchRequirement()"
            );
#endif
            // Can't entirely rely on this one check as it's state may change mid-game
            if ( researchProjectDef.prerequisites != null )
            {
                // Fast and easy
                return true;
            }

            // Check for an advanced research unlock
            return
                Controller.Data.AdvancedResearchDefs.Any( a => (
                    ( a.IsResearchToggle )&&
                    ( !a.HideDefs )&&
                    ( a.effectedResearchDefs.Contains( researchProjectDef ) )
                ) );
        }

        #endregion Availability

        #region Lists of affected data

        public static List<ResearchProjectDef> ExclusiveDescendants( this ResearchProjectDef research )
        {
            List<ResearchProjectDef> descendants = new List<ResearchProjectDef>();

            // recursively go through all children
            // populate initial queue
            Queue<ResearchProjectDef> queue = new Queue<ResearchProjectDef>( DefDatabase<ResearchProjectDef>.AllDefsListForReading.Where( res => res.prerequisites.Contains( research ) ) );

            // for each item in queue, determine if there's something unlocking it
            // if not, add to the list, and queue up children.
            while ( queue.Count > 0 )
            {
                ResearchProjectDef current = queue.Dequeue();

                if ( !Controller.Data.AdvancedResearchDefs.Any(
                        ard => ard.IsResearchToggle &&
                               !ard.HideDefs &&
                               !ard.IsLockedOut() &&
                               ard.effectedResearchDefs.Contains( current ) ) &&
                     !descendants.Contains( current ) )
                {
                    descendants.Add( current );
                    foreach ( ResearchProjectDef descendant in DefDatabase<ResearchProjectDef>.AllDefsListForReading.Where( res => res.prerequisites.Contains( current ) ) )
                    {
                        queue.Enqueue( descendant );
                    }
                }
            }

            return descendants;
        }

        public static List<ResearchProjectDef> GetPrerequisitesRecursive( this ResearchProjectDef research )
        {
            List<ResearchProjectDef> result = new List<ResearchProjectDef>();
            if ( research.prerequisites.NullOrEmpty() )
            {
                return result;
            }
            Stack<ResearchProjectDef> stack = new Stack<ResearchProjectDef>( research.prerequisites.Where( parent => parent != research ) );

            while ( stack.Count > 0 )
            {
                var parent = stack.Pop();
                result.Add( parent );

                if ( !parent.prerequisites.NullOrEmpty() )
                {
                    foreach ( var grandparent in parent.prerequisites )
                    {
                        if ( grandparent != parent )
                            stack.Push( grandparent );
                    }
                }
            }

            return result.Distinct().ToList();
        }

        public static List<Pair<Def, string>> GetUnlockDefsAndDescs( this ResearchProjectDef research )
        {
            if ( _unlocksCache.ContainsKey( research ) )
            {
                return _unlocksCache[research];
            }

            List<Pair<Def, string>> unlocks = new List<Pair<Def, string>>();

            // dumps recipes/plants unlocked, because of the peculiar way CCL helpdefs are done.
            List<ThingDef> dump = new List<ThingDef>();

            unlocks.AddRange( research.GetThingsUnlocked()
                                      .Where( d => d.IconTexture() != null )
                                      .Select( d => new Pair<Def, string>( d, "Fluffy.ResearchTree.AllowsBuildingX".Translate( d.LabelCap ) ) ) );
            unlocks.AddRange( research.GetTerrainUnlocked()
                                      .Where( d => d.IconTexture() != null )
                                      .Select( d => new Pair<Def, string>( d, "Fluffy.ResearchTree.AllowsBuildingX".Translate( d.LabelCap ) ) ) );
            unlocks.AddRange( research.GetRecipesUnlocked( ref dump )
                                      .Where( d => d.IconTexture() != null )
                                      .Select( d => new Pair<Def, string>( d, "Fluffy.ResearchTree.AllowsCraftingX".Translate( d.LabelCap ) ) ) );
            string sowTags = string.Join( " and ", research.GetSowTagsUnlocked( ref dump ).ToArray() );
            unlocks.AddRange( dump.Where( d => d.IconTexture() != null )
                                  .Select( d => new Pair<Def, string>( d, "Fluffy.ResearchTree.AllowsSowingXinY".Translate( d.LabelCap, sowTags ) ) ) );

            _unlocksCache.Add( research, unlocks );
            return unlocks;
        }

        public static Node Node( this ResearchProjectDef research )
        {
            return ResearchTree.ResearchTree.Forest.FirstOrDefault( node => node.Research == research );
        }

        public static List<Def> GetResearchRequirements( this ResearchProjectDef researchProjectDef )
        {
#if DEBUG
            CCL_Log.TraceMod(
                researchProjectDef,
                Verbosity.Stack,
                "GetResearchRequirements()"
            );
#endif
            var researchDefs = new List<Def>();

            if ( researchProjectDef.prerequisites != null )
            {
                if ( !researchProjectDef.prerequisites.Contains( Research.Locker ) )
                {
                    researchDefs.AddRange( researchProjectDef.prerequisites.ConvertAll<Def>( def => (Def)def ) );
                }
                else
                {
                    var advancedResearchDefs = Controller.Data.AdvancedResearchDefs.Where( a => (
                        ( a.IsResearchToggle )&&
                        ( !a.HideDefs )&&
                        ( a.effectedResearchDefs.Contains( researchProjectDef ) )
                    ) ).ToList();

                    if ( !advancedResearchDefs.NullOrEmpty() )
                    {
                        foreach ( var advancedResearchDef in advancedResearchDefs )
                        {
                            researchDefs.AddRange( advancedResearchDef.researchDefs.ConvertAll<Def>( def => (Def)def ) );
                        }
                    }
                }
            }

            // Return the list of research required
            return researchDefs;
        }

        public static List<Def> GetResearchUnlocked( this ResearchProjectDef researchProjectDef )
        {
#if DEBUG
            CCL_Log.TraceMod(
                researchProjectDef,
                Verbosity.Stack,
                "GetResearchUnlocked()"
            );
#endif
            var researchDefs = new List<Def>();

            //CCL_Log.Message("Normal");
            researchDefs.AddRange( DefDatabase<ResearchProjectDef>.AllDefsListForReading.Where( rd => rd.prerequisites.Contains( researchProjectDef ) ).ToList().ConvertAll<Def>( def => (Def)def ) );

            //CCL_Log.Message("Advanced");
            // same as prerequisites, but with effectedResearchDefs and researchDefs switched.
            var advancedResearchDefs = Controller.Data.AdvancedResearchDefs.Where( a => (
                 ( a.IsResearchToggle ) &&
                 ( !a.HideDefs ) &&
                 ( a.researchDefs.Contains( researchProjectDef ) )
             ) ).ToList();

            researchDefs.AddRange( advancedResearchDefs.SelectMany( ar => ar.effectedResearchDefs ).ToList().ConvertAll<Def>( Def => (Def)Def ) );

            return researchDefs;
        }

        public static List<Def> GetResearchedLockedBy( this ResearchProjectDef researchProjectDef )
        {
#if DEBUG
            CCL_Log.TraceMod(
                researchProjectDef,
                Verbosity.Stack,
                "GetResearchLockedBy()"
            );
#endif
            // Advanced Research that locks it
            var researchDefs = new List<Def>();

            // Look in advanced research
            var advancedResearch = Controller.Data.AdvancedResearchDefs.Where( a => (
                ( a.IsResearchToggle )&&
                ( a.HideDefs )&&
                ( a.effectedResearchDefs.Contains( researchProjectDef ) )
            ) ).ToList();

            // Aggregate research
            if ( !advancedResearch.NullOrEmpty() )
            {
                foreach ( var a in advancedResearch )
                {
                    researchDefs.AddRange( a.researchDefs.ConvertAll<Def>( def => (Def)def ) );
                }
            }

            return researchDefs;
        }

        public static List<ThingDef> GetThingsUnlocked( this ResearchProjectDef researchProjectDef )
        {
#if DEBUG
            CCL_Log.TraceMod(
                researchProjectDef,
                Verbosity.Stack,
                "GetThingsUnlocked()"
            );
#endif
            // Buildings it unlocks
            var thingsOn = new List<ThingDef>();
            var researchThings = DefDatabase<ThingDef>.AllDefsListForReading.Where( t => (
                ( !t.IsLockedOut() )&&
                ( t.researchPrerequisite != null )&&
                ( t.researchPrerequisite == researchProjectDef )
            ) ).ToList();

            if ( !researchThings.NullOrEmpty() )
            {
                thingsOn.AddRange( researchThings );
            }

            // Look in advanced research too
            var advancedResearch = Controller.Data.AdvancedResearchDefs.Where( a => (
                ( a.IsBuildingToggle )&&
                ( !a.HideDefs )&&
                ( a.researchDefs.Count == 1 )&&
                ( a.researchDefs.Contains( researchProjectDef ) )
            ) ).ToList();

            // Aggregate research
            if ( !advancedResearch.NullOrEmpty() )
            {
                foreach ( var a in advancedResearch )
                {
                    thingsOn.AddRange( a.thingDefs );
                }
            }

            return thingsOn;
        }

        public static List<TerrainDef> GetTerrainUnlocked( this ResearchProjectDef researchProjectDef )
        {
#if DEBUG
            CCL_Log.TraceMod(
                researchProjectDef,
                Verbosity.Stack,
                "GetTerrainUnlocked()"
            );
#endif
            // Buildings it unlocks
            var thingsOn = new List<TerrainDef>();
            var researchThings = DefDatabase<TerrainDef>.AllDefsListForReading.Where( t => (
                ( !t.IsLockedOut() )&&
                ( t.researchPrerequisite != null )&&
                ( t.researchPrerequisite == researchProjectDef )
            ) ).ToList();

            if ( !researchThings.NullOrEmpty() )
            {
                thingsOn.AddRange( researchThings );
            }

            return thingsOn;
        }

        public static List<RecipeDef> GetRecipesUnlocked( this ResearchProjectDef researchProjectDef, ref List<ThingDef> thingDefs )
        {
#if DEBUG
            CCL_Log.TraceMod(
                researchProjectDef,
                Verbosity.Stack,
                "GetRecipesUnlocked()"
            );
#endif
            // Recipes on buildings it unlocks
            var recipes = new List<RecipeDef>();
            if ( thingDefs != null )
            {
                thingDefs.Clear();
            }

            // Add all recipes using this research projects
            var researchRecipes = DefDatabase<RecipeDef>.AllDefsListForReading.Where( d => (
                ( d.researchPrerequisite == researchProjectDef )
            ) ).ToList();

            if ( !researchRecipes.NullOrEmpty() )
            {
                recipes.AddRange( researchRecipes );
            }

            if ( thingDefs != null )
            {
                // Add buildings for those recipes
                foreach ( var r in recipes )
                {
                    thingDefs.AddRange( r.recipeUsers );
                }
            }

            // Look in advanced research too
            var advancedResearch = Controller.Data.AdvancedResearchDefs.Where( a => (
                ( a.IsRecipeToggle )&&
                ( !a.HideDefs )&&
                ( a.researchDefs.Count == 1 )&&
                ( a.researchDefs.Contains( researchProjectDef ) )
            ) ).ToList();

            // Aggregate research
            if ( !advancedResearch.NullOrEmpty() )
            {
                foreach ( var a in advancedResearch )
                {
                    recipes.AddRange( a.recipeDefs );
                    if ( thingDefs != null )
                    {
                        thingDefs.AddRange( a.thingDefs );
                    }
                }
            }

            return recipes;
        }

        public static List<RecipeDef> GetRecipesLocked( this ResearchProjectDef researchProjectDef, ref List<ThingDef> thingDefs )
        {
#if DEBUG
            CCL_Log.TraceMod(
                researchProjectDef,
                Verbosity.Stack,
                "GetRecipesLocked()"
            );
#endif
            // Recipes on buildings it locks
            var recipes = new List<RecipeDef>();
            if ( thingDefs != null )
            {
                thingDefs.Clear();
            }

            // Look in advanced research
            var advancedResearch = Controller.Data.AdvancedResearchDefs.Where( a => (
                ( a.IsRecipeToggle )&&
                ( a.HideDefs )&&
                ( a.researchDefs.Count == 1 )&&
                ( a.researchDefs.Contains( researchProjectDef ) )
            ) ).ToList();

            // Aggregate research
            if ( !advancedResearch.NullOrEmpty() )
            {
                foreach ( var a in advancedResearch )
                {
                    recipes.AddRange( a.recipeDefs );
                    if ( thingDefs != null )
                    {
                        thingDefs.AddRange( a.thingDefs );
                    }
                }
            }

            return recipes;
        }

        public static List<string> GetSowTagsUnlocked( this ResearchProjectDef researchProjectDef, ref List<ThingDef> thingDefs )
        {
#if DEBUG
            CCL_Log.TraceMod(
                researchProjectDef,
                Verbosity.Stack,
                "GetSowTagsUnlocked()"
            );
#endif
            var sowTags = new List<string>();
            if ( thingDefs != null )
            {
                thingDefs.Clear();
            }

            // Look in advanced research to add plants and sow tags it unlocks
            var advancedResearch = Controller.Data.AdvancedResearchDefs.Where( a => (
                ( a.IsPlantToggle )&&
                ( !a.HideDefs )&&
                ( a.researchDefs.Count == 1 )&&
                ( a.researchDefs.Contains( researchProjectDef ) )
            ) ).ToList();

            // Aggregate advanced research
            if ( !advancedResearch.NullOrEmpty() )
            {
                foreach ( var a in advancedResearch )
                {
                    sowTags.AddRange( a.sowTags );
                    if ( thingDefs != null )
                    {
                        thingDefs.AddRange( a.thingDefs );
                    }
                }
            }

            return sowTags;
        }

        public static List<string> GetSowTagsLocked( this ResearchProjectDef researchProjectDef, ref List<ThingDef> thingDefs )
        {
#if DEBUG
            CCL_Log.TraceMod(
                researchProjectDef,
                Verbosity.Stack,
                "GetSowTagsLocked()"
            );
#endif
            var sowTags = new List<string>();
            if ( thingDefs != null )
            {
                thingDefs.Clear();
            }

            // Look in advanced research to add plants and sow tags it unlocks
            var advancedResearch = Controller.Data.AdvancedResearchDefs.Where( a => (
                ( a.IsPlantToggle )&&
                ( a.HideDefs )&&
                ( a.researchDefs.Count == 1 )&&
                ( a.researchDefs.Contains( researchProjectDef ) )
            ) ).ToList();

            // Aggregate advanced research
            if ( !advancedResearch.NullOrEmpty() )
            {
                foreach ( var a in advancedResearch )
                {
                    sowTags.AddRange( a.sowTags );
                    if ( thingDefs != null )
                    {
                        thingDefs.AddRange( a.thingDefs );
                    }
                }
            }

            return sowTags;
        }

        #endregion Lists of affected data
    }
}