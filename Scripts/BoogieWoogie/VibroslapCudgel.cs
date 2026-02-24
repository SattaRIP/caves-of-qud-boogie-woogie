using System;
using XRL.World.Parts.Mutation;
using XRL.World.Anatomy;

namespace XRL.World.Parts
{
    [Serializable]
    public class VibroslapCudgel : IPart
    {
        public int MutationLevel = 3;
        public int FreeActionChance = 30;
        public bool GrantedMutation = false;

        public override bool SameAs(IPart p)
        {
            VibroslapCudgel other = p as VibroslapCudgel;
            if (other == null)
                return false;

            return other.MutationLevel == MutationLevel &&
                   other.FreeActionChance == FreeActionChance;
        }

        public override bool WantEvent(int ID, int cascade)
        {
            return base.WantEvent(ID, cascade)
                || ID == EquippedEvent.ID
                || ID == UnequippedEvent.ID;
        }

        public override bool HandleEvent(EquippedEvent E)
        {
            GameObject wearer = E.Actor;
            if (wearer == null)
                return base.HandleEvent(E);

            // Get or create Mutations part
            Mutations mutations = wearer.RequirePart<Mutations>();
            if (mutations == null)
                return base.HandleEvent(E);

            // Grant or level up Boogie Woogie mutation
            BaseMutation existing = mutations.GetMutation("BoogieWoogie");
            if (existing == null)
            {
                // Grant new mutation
                mutations.AddMutation("BoogieWoogie", MutationLevel);
                GrantedMutation = true;
            }
            else if (existing.BaseLevel < MutationLevel)
            {
                // Level up existing mutation to our level
                existing.ChangeLevel(MutationLevel - existing.BaseLevel);
                GrantedMutation = false;  // We didn't grant it, just leveled it
            }

            // Set vibroslap properties
            wearer.SetIntProperty("HasVibroslapEquipped", 1);
            wearer.SetIntProperty("VibroslapFreeActionChance", FreeActionChance);

            return base.HandleEvent(E);
        }

        public override bool HandleEvent(UnequippedEvent E)
        {
            GameObject wearer = E.Actor;
            if (wearer == null)
                return base.HandleEvent(E);

            // Check if wearer has other vibroslap items equipped/implanted
            bool hasOtherVibroslap = false;

            // Check equipped items
            Body body = wearer.GetPart<Body>();
            if (body != null)
            {
                foreach (BodyPart part in body.GetParts())
                {
                    GameObject equipped = part.Equipped;
                    if (equipped != null && equipped != ParentObject)
                    {
                        if (equipped.HasPart("VibroslapCudgel") || equipped.HasPart("VibroslapCybernetics"))
                        {
                            hasOtherVibroslap = true;
                            break;
                        }
                    }
                }
            }

            // Check cybernetic implants via body parts
            if (body != null && !hasOtherVibroslap)
            {
                foreach (BodyPart part in body.GetParts())
                {
                    if (part.Cybernetics != null && part.Cybernetics.HasPart("VibroslapCybernetics"))
                    {
                        hasOtherVibroslap = true;
                        break;
                    }
                }
            }

            // Only remove mutation and clear properties if no other vibroslap items
            if (!hasOtherVibroslap)
            {
                // Remove mutation if we granted it
                if (GrantedMutation)
                {
                    Mutations mutations = wearer.GetPart<Mutations>();
                    if (mutations != null)
                    {
                        BaseMutation bw = mutations.GetMutation("BoogieWoogie");
                        if (bw != null)
                        {
                            mutations.RemoveMutation(bw);
                        }
                    }
                }

                // Clear vibroslap properties
                wearer.RemoveIntProperty("HasVibroslapEquipped");
                wearer.RemoveIntProperty("VibroslapFreeActionChance");
                wearer.RemoveIntProperty("VibroslapCyberneticsBonus");
            }

            GrantedMutation = false;
            return base.HandleEvent(E);
        }
    }
}
