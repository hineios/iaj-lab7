﻿using Assets.Scripts.IAJ.Unity.DecisionMaking.GOB;
using Action = Assets.Scripts.IAJ.Unity.DecisionMaking.GOB.Action;
using Assets.Scripts.GameManager;

namespace Assets.Scripts.DecisionMakingActions
{
    public class LevelUp : Action
    {
        public AutonomousCharacter Character { get; private set; }

        public LevelUp(AutonomousCharacter character) : base("LevelUp")
        {
            this.Character = character;
        }

		public override void ApplyActionEffects(EfficientWorldModel worldModel)
        {
            int maxHP = (int)worldModel.GetProperty(Properties.MAXHP);
            int level = (int)worldModel.GetProperty(Properties.LEVEL);

            worldModel.SetProperty(Properties.LEVEL, level + 1);
            worldModel.SetProperty(Properties.MAXHP, maxHP + 10);
            worldModel.SetProperty(Properties.HP, maxHP + 10);
        }

        public override bool CanExecute()
        {
            var level = this.Character.GameManager.characterData.Level;
            var xp = this.Character.GameManager.characterData.XP;

            if(level == 1)
            {
                return xp >= 10;
            }
            else if(level == 2)
            {
                return xp >= 30;
            }

            return false;
        }
        

		public override bool CanExecute(EfficientWorldModel worldModel)
        {
            int xp = (int)worldModel.GetProperty(Properties.XP);
            int level = (int)worldModel.GetProperty(Properties.LEVEL);

            if (level == 1)
            {
                return xp >= 10;
            }
            else if (level == 2)
            {
                return xp >= 30;
            }

            return false;
        }

        public override void Execute()
        {
            this.Character.GameManager.LevelUp();
        }

        public override float GetDuration()
        {
            return 0.0f;
        }

		public override float GetDuration(EfficientWorldModel worldModel)
        {
            return 0.0f;
        }

        public override float GetGoalChange(Goal goal)
        {
            return 0.0f;
        }

		public override float GetH(EfficientWorldModel currentState)
		{
            //This action has no duration
            //Since this method is only called if the action is executable, we should do it right away
            //It has only benefits increase MaxHP and heal to new MaxHP
            return 0.0f;
		}
    }
}
