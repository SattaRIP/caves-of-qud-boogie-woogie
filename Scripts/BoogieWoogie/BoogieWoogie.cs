using System;
using System.Collections.Generic;
using XRL.UI;
using XRL.World.Anatomy;
using XRL.World.Effects;
using XRL.Rules;

namespace XRL.World.Parts.Mutation
{
    [Serializable]
    public class BoogieWoogie : BaseMutation
    {
        public const string CMD_BOOGIE_WOOGIE = "CommandBoogieWoogie";
        public const string CMD_BOOGIE_WOOGIE_REPEAT = "CommandBoogieWoogieRepeat";
        public Guid BoogieWoogieAbilityID = Guid.Empty;
        public Guid BoogieWoogieRepeatAbilityID = Guid.Empty;

        public BoogieWoogie()
        {
            // DisplayName now set in Mutations.xml
        }

        // ===== REGISTRATION =====

        public override bool AllowStaticRegistration()
        {
            return true;
        }

        public override void Register(GameObject Object, IEventRegistrar Registrar)
        {
            Registrar.Register(CMD_BOOGIE_WOOGIE);
            Registrar.Register(CMD_BOOGIE_WOOGIE_REPEAT);
            base.Register(Object, Registrar);
        }

        // ===== EVENT HANDLING =====

        public override bool FireEvent(Event E)
        {
            if (E.ID == CMD_BOOGIE_WOOGIE)
            {
                DoBoogieWoogie();
                return true;
            }
            if (E.ID == CMD_BOOGIE_WOOGIE_REPEAT)
            {
                DoBoogieWoogieRepeat();
                return true;
            }
            return base.FireEvent(E);
        }

        // ===== MAIN ABILITY LOGIC =====

        private bool DoBoogieWoogie()
        {
            // Step 1: Check hand count and determine mode
            int freeHandCount = CountFreeHands();

            if (freeHandCount == 0)
            {
                if (ParentObject.IsPlayer())
                {
                    Popup.Show("You need at least one free hand to use Boogie Woogie!");
                }
                return false;
            }

            bool twoHandMode = freeHandCount >= 2;
            int range = GetRange(Level);

            // Step 2: One-hand mode requires melee attack first
            if (!twoHandMode)
            {
                if (ParentObject.IsPlayer())
                {
                    Popup.Show("{{y|One hand free - you must clap an enemy with a melee attack!}}");
                }

                // Pick enemy to attack
                Cell attackCell = PickDestinationCell(
                    1, // Melee range only
                    RequireCombat: true,
                    Label: "Boogie Woogie: Clap (melee attack) enemy",
                    Snap: true
                );

                if (attackCell == null)
                    return false;

                GameObject enemy = attackCell.GetCombatTarget(ParentObject, false, false, false, 0);
                if (enemy == null)
                {
                    if (ParentObject.IsPlayer())
                    {
                        Popup.Show("No enemy to attack in that cell.");
                    }
                    return false;
                }

                // Perform melee attack
                ParentObject.PerformMeleeAttack(enemy);

                if (ParentObject.IsPlayer())
                {
                    XRL.Messages.MessageQueue.AddPlayerMessage("{{M|*clap*}} You strike " + enemy.the + enemy.ShortDisplayName + "!");
                }
            }

            // Step 3: Pick first target to swap
            Cell cell1 = PickDestinationCell(
                range,
                RequireCombat: false,
                Label: "Boogie Woogie: First target",
                Snap: true
            );

            if (cell1 == null)
                return false;

            GameObject target1 = GetValidTarget(cell1);
            if (target1 == null)
            {
                if (ParentObject.IsPlayer())
                {
                    Popup.Show("No valid target in that cell.");
                }
                return false;
            }

            // Step 4: Pick second target
            Cell cell2 = PickDestinationCell(
                range,
                RequireCombat: false,
                Label: "Boogie Woogie: Second target",
                Snap: true
            );

            if (cell2 == null)
                return false;

            GameObject target2 = GetValidTarget(cell2);
            if (target2 == null)
            {
                if (ParentObject.IsPlayer())
                {
                    Popup.Show("No valid target in that cell.");
                }
                return false;
            }

            // Step 5: Validate targets
            if (!ValidateTargets(target1, target2))
                return false;

            // Step 6: Execute swap
            if (!ExecuteSwap(target1, target2, twoHandMode))
                return false;

            // Step 7: Consume resources (only if we didn't already attack in one-hand mode)
            if (twoHandMode)
            {
                // Check for vibroslap free action chance
                int freeChance = GetVibroslapFreeActionChance();
                bool freeAction = freeChance > 0 && Stat.Random(1, 100) <= freeChance;

                if (!freeAction)
                {
                    ParentObject.UseEnergy(1000, "Mental Mutation BoogieWoogie");
                }
                else if (ParentObject.IsPlayer())
                {
                    XRL.Messages.MessageQueue.AddPlayerMessage("{{G|Your vibroslap resonates perfectly - free action!}}");
                }
            }
            // In one-hand mode, the melee attack already consumed energy

            CooldownMyActivatedAbility(BoogieWoogieAbilityID, GetCooldown(Level));

            return true;
        }

        // ===== HAND VALIDATION =====

        private int CountFreeHands()
        {
            // Vibroslap items bypass hand requirements
            if (HasVibroslapEquipped())
                return 2;

            // First check basic movement capability
            if (!ParentObject.CanMoveExtremities("Clap", true, false, false))
                return 0;

            Body body = ParentObject.GetPart<Body>();
            if (body == null)
                return 0;

            int freeHandCount = 0;

            // Iterate through all body parts looking for hands
            foreach (BodyPart part in body.GetParts())
            {
                // Check if this is a hand-type part
                if (!IsHandType(part.Type))
                    continue;

                // Check if hand is free (not holding equipment)
                if (part.Equipped == null)
                {
                    freeHandCount++;
                }
            }

            return freeHandCount;
        }

        private bool IsHandType(string partType)
        {
            // Hand types that can clap
            return partType == "Hand" ||
                   partType == "Hands" ||
                   partType == "Claw" ||
                   partType == "Pincers" ||
                   partType == "Manipulator";
        }

        // ===== VIBROSLAP DETECTION =====

        private bool HasVibroslapEquipped()
        {
            return ParentObject.GetIntProperty("HasVibroslapEquipped") == 1;
        }

        private int GetVibroslapFreeActionChance()
        {
            return ParentObject.GetIntProperty("VibroslapFreeActionChance");
        }

        private int GetVibroslapConfusionBonus()
        {
            return ParentObject.GetIntProperty("VibroslapCyberneticsBonus");
        }

        // ===== CONFUSION EFFECT =====

        private void TryApplyConfusion(GameObject target)
        {
            // Only apply to creatures with brains that aren't allied
            if (!target.HasPart("Brain") || target.IsAlliedTowards(ParentObject))
                return;

            // Calculate confusion chance: 20 + (Level * 5)%, capped at 95%
            int chance = Math.Min(20 + (Level * 5), 95);
            if (Stat.Random(1, 100) > chance)
                return;

            // Calculate save difficulty: 10 + (Level * 2) + Ego + Cybernetic Bonus
            int difficulty = 10 + (Level * 2) + ParentObject.StatMod("Ego") + GetVibroslapConfusionBonus();

            // Calculate duration: 3 + (Level / 2) rounds
            int duration = 3 + (Level / 2);

            // Apply Confusion if target fails Willpower save
            if (!target.MakeSave("Willpower", difficulty, ParentObject, "Ego"))
            {
                target.ApplyEffect(new Confused(duration, difficulty, difficulty));

                if (ParentObject.IsPlayer())
                {
                    XRL.Messages.MessageQueue.AddPlayerMessage(
                        target.The + target.ShortDisplayName + " " + target.GetVerb("are") + " {{M|confused}} by the spatial shift!"
                    );
                }
            }
            else if (ParentObject.IsPlayer())
            {
                XRL.Messages.MessageQueue.AddPlayerMessage(
                    target.The + target.ShortDisplayName + " " + target.GetVerb("resist") + " the confusion."
                );
            }
        }

        // ===== TARGET VALIDATION =====

        private List<GameObject> GetAllValidTargets(Cell cell)
        {
            List<GameObject> validTargets = new List<GameObject>();

            if (cell == null)
                return validTargets;

            // Collect all valid objects in the cell
            foreach (GameObject obj in cell.Objects)
            {
                // Skip non-physical objects
                if (!obj.HasPart("Physics"))
                    continue;

                Physics physics = obj.GetPart<Physics>();
                if (physics == null || !physics.IsReal)
                    continue;

                validTargets.Add(obj);
            }

            return validTargets;
        }

        private GameObject GetValidTarget(Cell cell)
        {
            if (cell == null)
                return null;

            List<GameObject> validTargets = GetAllValidTargets(cell);

            if (validTargets.Count == 0)
                return null;

            // If only one target, return it
            if (validTargets.Count == 1)
                return validTargets[0];

            // Multiple targets - let player choose
            if (ParentObject.IsPlayer())
            {
                List<string> options = new List<string>();
                foreach (GameObject obj in validTargets)
                {
                    options.Add(obj.a + obj.ShortDisplayName);
                }

                int choice = Popup.ShowOptionList(
                    "Select target",
                    options.ToArray(),
                    null,
                    0,
                    "Multiple objects in that cell. Choose which to swap:"
                );

                if (choice >= 0 && choice < validTargets.Count)
                    return validTargets[choice];

                return null; // Player cancelled
            }

            // For NPCs, just pick the first one
            return validTargets[0];
        }

        private bool ValidateTargets(GameObject target1, GameObject target2)
        {
            // Null checks
            if (target1 == null || target2 == null)
            {
                if (ParentObject.IsPlayer())
                {
                    Popup.Show("Invalid target.");
                }
                return false;
            }

            // Same object check
            if (target1 == target2)
            {
                if (ParentObject.IsPlayer())
                {
                    Popup.Show("You cannot swap a target with itself.");
                }
                return false;
            }

            // Validate objects still exist and are valid
            if (!GameObject.Validate(target1) || !GameObject.Validate(target2))
            {
                if (ParentObject.IsPlayer())
                {
                    Popup.Show("One of the targets is no longer valid.");
                }
                return false;
            }

            // Check if objects can be moved (not anchored, etc.)
            if (target1.HasEffect("Anchored") || target2.HasEffect("Anchored"))
            {
                if (ParentObject.IsPlayer())
                {
                    Popup.Show("An anchored target cannot be swapped.");
                }
                return false;
            }

            return true;
        }

        // ===== SWAP EXECUTION =====

        private bool ExecuteSwap(GameObject target1, GameObject target2, bool twoHandMode)
        {
            // Store original positions
            Cell cell1 = target1.CurrentCell;
            Cell cell2 = target2.CurrentCell;

            if (cell1 == null || cell2 == null)
            {
                if (ParentObject.IsPlayer())
                {
                    Popup.Show("One of the targets has no valid position.");
                }
                return false;
            }

            // Execute the swap using TeleportTo for exact positioning
            // We need to be careful with the order to avoid conflicts

            // Temporarily remove both objects from their cells
            cell1.RemoveObject(target1);
            cell2.RemoveObject(target2);

            // Move target1 to cell2 position
            bool success1 = target1.DirectMoveTo(cell2);

            // Move target2 to cell1 position
            bool success2 = target2.DirectMoveTo(cell1);

            // If either failed, report error
            if (!success1 || !success2)
            {
                if (ParentObject.IsPlayer())
                {
                    Popup.Show("The swap was disrupted!");
                }
                return false;
            }

            // Clear targeting on both swapped targets
            ClearTargeting(target1);
            ClearTargeting(target2);

            // Try to apply confusion to non-allies
            TryApplyConfusion(target1);
            TryApplyConfusion(target2);

            // Success message
            if (ParentObject.IsPlayer())
            {
                string msg = string.Format(
                    "{{M|*clap*}} You swap {0} and {1}!",
                    target1.the + target1.ShortDisplayName,
                    target2.the + target2.ShortDisplayName
                );
                XRL.Messages.MessageQueue.AddPlayerMessage(msg);
            }

            return true;
        }

        // ===== TARGET CLEARING =====

        private void ClearTargeting(GameObject target)
        {
            if (target == null)
                return;

            // Find all objects in the zone that might be targeting this object
            Zone zone = target.CurrentZone;
            if (zone == null)
                return;

            foreach (GameObject obj in zone.GetObjects())
            {
                // Check if this object has a Brain part (creatures that can target)
                Brain brain = obj.GetPart<Brain>();
                if (brain != null)
                {
                    // Clear this target if it was being targeted
                    if (brain.Target == target)
                    {
                        brain.Target = null;
                    }
                }

                // Targeting effects cleared via Brain.Target = null above
            }
        }

        // ===== BOOGIE WOOGIE REPEAT =====

        private bool DoBoogieWoogieRepeat()
        {
            // Require vibroslap to use this ability
            if (!HasVibroslapEquipped())
            {
                if (ParentObject.IsPlayer())
                {
                    Popup.Show("You need a vibroslap to use Boogie Woogie Repeat!");
                }
                return false;
            }

            int range = GetRange(Level);
            List<GameObject> targets = new List<GameObject>();
            List<Cell> originalCells = new List<Cell>();

            // Step 1: Mark all targets
            if (ParentObject.IsPlayer())
            {
                Popup.Show("{{y|Mark all targets to randomize. Press ESC when done.}}");
            }

            while (true)
            {
                Cell targetCell = PickDestinationCell(
                    range,
                    RequireCombat: false,
                    Label: "Boogie Woogie Repeat: Mark target (" + targets.Count + " marked, ESC to finish)",
                    Snap: true
                );

                // ESC pressed or invalid cell
                if (targetCell == null)
                    break;

                GameObject target = GetValidTarget(targetCell);
                if (target == null)
                {
                    if (ParentObject.IsPlayer())
                    {
                        Popup.Show("No valid target in that cell.");
                    }
                    continue;
                }

                // Check if already marked
                if (targets.Contains(target))
                {
                    if (ParentObject.IsPlayer())
                    {
                        Popup.Show("Target already marked.");
                    }
                    continue;
                }

                // Add target
                targets.Add(target);
                originalCells.Add(target.CurrentCell);

                if (ParentObject.IsPlayer())
                {
                    XRL.Messages.MessageQueue.AddPlayerMessage(
                        "Marked " + target.the + target.ShortDisplayName + " ({{Y|" + targets.Count + "}} total)"
                    );
                }
            }

            // Step 2: Validate minimum targets
            if (targets.Count < 2)
            {
                if (ParentObject.IsPlayer())
                {
                    Popup.Show("You need to mark at least 2 targets.");
                }
                return false;
            }

            // Step 3: Validate all targets still exist and have valid positions
            for (int i = 0; i < targets.Count; i++)
            {
                if (!GameObject.Validate(targets[i]) || targets[i].CurrentCell == null)
                {
                    if (ParentObject.IsPlayer())
                    {
                        Popup.Show("One of the marked targets is no longer valid.");
                    }
                    return false;
                }
            }

            // Step 4: Perform multiple rapid shuffles (3-5 times)
            int shuffleCount = Stat.Random(3, 5);

            for (int round = 0; round < shuffleCount; round++)
            {
                // Create randomized position list for this round
                List<Cell> currentCells = new List<Cell>();
                foreach (GameObject target in targets)
                {
                    currentCells.Add(target.CurrentCell);
                }

                List<Cell> shuffledCells = new List<Cell>(currentCells);

                // Fisher-Yates shuffle
                for (int i = shuffledCells.Count - 1; i > 0; i--)
                {
                    int j = Stat.Random(0, i);
                    Cell temp = shuffledCells[i];
                    shuffledCells[i] = shuffledCells[j];
                    shuffledCells[j] = temp;
                }

                // Remove all targets from their cells temporarily
                foreach (GameObject target in targets)
                {
                    target.CurrentCell.RemoveObject(target);
                }

                // Move all targets to their new randomized positions
                for (int i = 0; i < targets.Count; i++)
                {
                    shuffledCells[i].AddObject(targets[i]);
                }

                // Visual feedback for each shuffle
                if (ParentObject.IsPlayer() && round < shuffleCount - 1)
                {
                    XRL.Messages.MessageQueue.AddPlayerMessage("{{M|*CLAP*}}");
                }
            }

            // Step 5: Clear targeting and apply confusion to all swapped targets
            foreach (GameObject target in targets)
            {
                ClearTargeting(target);
                TryApplyConfusion(target);
            }

            // Step 6: Success message
            if (ParentObject.IsPlayer())
            {
                XRL.Messages.MessageQueue.AddPlayerMessage(
                    "{{M|*CLAP CLAP CLAP*}} You rapidly shuffle {{Y|" + targets.Count + "}} targets {{M|" + shuffleCount + "}} times!"
                );
            }

            // Step 7: Check for free action
            int freeChance = GetVibroslapFreeActionChance();
            bool freeAction = freeChance > 0 && Stat.Random(1, 100) <= freeChance;

            if (!freeAction)
            {
                ParentObject.UseEnergy(1000, "Mental Mutation BoogieWoogieRepeat");
            }
            else if (ParentObject.IsPlayer())
            {
                XRL.Messages.MessageQueue.AddPlayerMessage("{{G|Your vibroslap resonates perfectly - free action!}}");
            }

            // Step 8: Apply cooldown
            CooldownMyActivatedAbility(BoogieWoogieRepeatAbilityID, GetRepeatCooldown(Level));

            return true;
        }

        // ===== LEVEL SCALING =====

        public int GetRange(int Level)
        {
            // Base 8, +2 per 2 levels, cap at 18
            return Math.Min(8 + (Level / 2) * 2, 18);
        }

        public int GetCooldown(int Level)
        {
            // No cooldown - limited by energy cost (1000 per use = 1 turn)
            return 0;
        }

        public int GetRepeatCooldown(int Level)
        {
            // Repeat ability has cooldown: 10 rounds at level 1, decreasing to 5 at level 15+
            return Math.Max(5, 10 - (Level / 3));
        }

        // ===== MUTATION LIFECYCLE =====

        public override bool Mutate(GameObject GO, int Level)
        {
            BoogieWoogieAbilityID = AddMyActivatedAbility(
                Name: "Boogie Woogie",
                Command: CMD_BOOGIE_WOOGIE,
                Class: "Mental Mutations",
                Description: "Swap two targets. 2 hands: clap. 1 hand: attack then swap.",
                Icon: "*"
            );

            BoogieWoogieRepeatAbilityID = AddMyActivatedAbility(
                Name: "Boogie Woogie Repeat",
                Command: CMD_BOOGIE_WOOGIE_REPEAT,
                Class: "Mental Mutations",
                Description: "Mark multiple targets and randomize their positions. Requires vibroslap.",
                Icon: "+"
            );

            return base.Mutate(GO, Level);
        }

        public override bool Unmutate(GameObject GO)
        {
            RemoveMyActivatedAbility(ref BoogieWoogieAbilityID);
            RemoveMyActivatedAbility(ref BoogieWoogieRepeatAbilityID);
            return base.Unmutate(GO);
        }

        // ===== DESCRIPTIONS =====

        public override string GetDescription()
        {
            return "You can swap the positions of two targets within your line of sight.\n\n" +
                   "{{y|Two hands free:}} Clap to swap any two targets.\n" +
                   "{{y|One hand free:}} Clap (melee attack) an enemy, then swap two targets.\n" +
                   "{{y|With vibroslap:}} Bypass hand requirements and unlock Boogie Woogie Repeat.\n\n" +
                   "Swapped targets may become {{M|confused}} by the spatial displacement.";
        }

        public override string GetLevelText(int Level)
        {
            int cooldown = GetCooldown(Level);
            string cooldownText = cooldown > 0 ? $"Cooldown: {cooldown} rounds\n" : "";

            int confusionChance = Math.Min(20 + (Level * 5), 95);
            int confusionDuration = 3 + (Level / 2);
            int confusionDC = 10 + (Level * 2);
            int repeatCooldown = GetRepeatCooldown(Level);

            return string.Format(
                "Range: {0} tiles\n" +
                "{1}" +
                "Confusion chance: {2}%\n" +
                "Confusion duration: {3} rounds\n" +
                "Confusion save DC: {4} + Ego modifier\n" +
                "Repeat cooldown: {5} rounds\n" +
                "{{r|Requires at least 1 free hand (unless using vibroslap)}}",
                GetRange(Level),
                cooldownText,
                confusionChance,
                confusionDuration,
                confusionDC,
                repeatCooldown
            );
        }
    }
}
