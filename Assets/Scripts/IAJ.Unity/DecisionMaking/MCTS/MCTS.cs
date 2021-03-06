﻿using Assets.Scripts.GameManager;
using Assets.Scripts.IAJ.Unity.DecisionMaking.GOB;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Assets.Scripts.IAJ.Unity.DecisionMaking.MCTS
{
    public class MCTS
    {
        public const float C = 1.4f;
        public bool InProgress { get; private set; }
        public int MaxIterations { get; set; }
        public int MaxIterationsProcessedPerFrame { get; set; }
        public int MaxPlayoutDepthReached { get; protected set; }
        public int MaxSelectionDepthReached { get; protected set; }
        public float TotalProcessingTime { get; protected set; }
        public MCTSNode BestFirstChild { get; set; }
        public List<GOB.Action> BestActionSequence { get; private set; }


        protected int CurrentIterations { get; set; }
        protected int CurrentIterationsInFrame { get; set; }
        protected int CurrentDepth { get; set; }

        protected CurrentStateWorldModel CurrentStateWorldModel { get; set; }
        protected MCTSNode InitialNode { get; set; }
        protected System.Random RandomGenerator { get; set; }

        public MCTS(CurrentStateWorldModel currentStateWorldModel)
        {
            this.InProgress = false;
            this.CurrentStateWorldModel = currentStateWorldModel;
            this.MaxIterations = 100;
            this.MaxIterationsProcessedPerFrame = 10;
            this.RandomGenerator = new System.Random();
        }


        public void InitializeMCTSearch()
        {
            this.MaxPlayoutDepthReached = 0;
            this.MaxSelectionDepthReached = 0;
            this.CurrentIterations = 0;
            this.CurrentIterationsInFrame = 0;
            this.TotalProcessingTime = 0.0f;
            this.CurrentStateWorldModel.Initialize();
            this.InitialNode = new MCTSNode(this.CurrentStateWorldModel)
            {
                Action = null,
                Parent = null,
                PlayerID = 0
            };
            this.InProgress = true;
            this.BestFirstChild = null;
            this.BestActionSequence = new List<GOB.Action>();
        }

        public GOB.Action Run()
        {
            MCTSNode selectedNode;
            Reward reward;
            this.CurrentIterationsInFrame = 0;
            var startTime = Time.realtimeSinceStartup;


            // This will be used to limit the time and memory used in the search. 
            while (this.CurrentIterationsInFrame < this.MaxIterationsProcessedPerFrame && this.CurrentIterations < this.MaxIterations)
            {
                // Selection: Select best node for the playout fase.
                selectedNode = Selection(this.InitialNode);

                // Playout: Drills randomly until a final state is achieved
				reward = Playout(selectedNode.State);

                // Backpropagate: Updates all the parent nodes until the root with the value obtained in the Playout phase 
                Backpropagate(selectedNode, reward);
                this.CurrentIterations += 1;
                this.CurrentIterationsInFrame += 1;
            }
            this.TotalProcessingTime += Time.realtimeSinceStartup - startTime;

            if (this.CurrentIterations >= this.MaxIterations)
                this.InProgress = false;

            this.BestFirstChild = BestChild(InitialNode);
            this.BestActionSequence.Clear();
            var node = this.BestFirstChild;
            while (true)
            {
                if (node == null)
                    break;
                this.BestActionSequence.Add(node.Action);
                node = BestChild(node);
            }

            if (this.BestFirstChild == null)
                return null;
            return this.BestFirstChild.Action;
        }

        protected MCTSNode Selection(MCTSNode initialNode)
        {
            var node = initialNode;
            int selectionReach = 0;
            GOB.Action a = null;
            while (!node.State.IsTerminal())
            {
                a = node.State.GetNextAction();
                if (a != null)
                {
                    return Expand(node, a);
                }
                else
                {
                    selectionReach += 1;
                    node = BestUCTChild(node);
                }
            }
            if (selectionReach > MaxSelectionDepthReached)
                MaxSelectionDepthReached = selectionReach;

            return node;
        }

		protected virtual Reward Playout(EfficientWorldModel initialPlayoutState)
        {
            
			EfficientWorldModel roll = initialPlayoutState.GenerateChildWorldModel();

            int playoutReach = 0;
            // Drills randomly (through a random child) until a terminal state is achieved
            while (!roll.IsTerminal())
            {
                // Gets a random action
                GOB.Action action = roll.GetExecutableActions()[RandomGenerator.Next(0, roll.GetExecutableActions().Length)];
                action.ApplyActionEffects(roll);
                roll.CalculateNextPlayer();
                playoutReach += 1;
            }

            if (playoutReach > MaxPlayoutDepthReached)
                MaxPlayoutDepthReached = playoutReach;

            // Gets the reward of that branch (the Score of that reached final state) 
            Reward reward = new Reward();
            reward.Value = roll.GetScore();
            reward.PlayerID = roll.GetNextPlayer();

            return reward;
        }

        protected virtual void Backpropagate(MCTSNode node, Reward reward)
        {
            // Drills up and updates each parent until the root with the reward value of the final node
            while (node != null)
            {
                node.N += 1;
                node.Q += reward.GetRewardForNode(node);
                node = node.Parent;
            }
        }

        protected MCTSNode Expand(MCTSNode parent, GOB.Action action)
        {
			EfficientWorldModel futureState = parent.State.GenerateChildWorldModel();
            action.ApplyActionEffects(futureState);
            futureState.CalculateNextPlayer();
            MCTSNode child = new MCTSNode(futureState);
            child.Parent = parent;
            child.Action = action;
            parent.ChildNodes.Add(child);
            return child;
        }

        //gets the best child of a node, using the UCT formula
        protected virtual MCTSNode BestUCTChild(MCTSNode node)
        {
            List<MCTSNode> bestChild = new List<MCTSNode>();
            double UCTValue, MaxUCTValue = float.MinValue;

            foreach (MCTSNode child in node.ChildNodes)
            {
                UCTValue = child.Q/child.N + C * Math.Sqrt(Math.Log(child.Parent.N) / node.N);
                
                if (UCTValue > MaxUCTValue)
                {
                    bestChild.Clear();
                    bestChild.Add(child);
                    MaxUCTValue = UCTValue;
                }else if(UCTValue == MaxUCTValue)
                {
                    bestChild.Add(child);
                }
            }
            if (bestChild.Count == 1)
                return bestChild[0];
            else if (bestChild.Count == 0)
                return null;
            else
                return bestChild[RandomGenerator.Next(0, bestChild.Count)];
        }

        //this method is very similar to the bestUCTChild, but it is used to return the final action of the MCTS search, and so we do not care about
        //the exploration factor
        protected MCTSNode BestChild(MCTSNode node)
        {
            List<MCTSNode> bestChild = new List<MCTSNode>();
            double UCTValue, MaxUCTValue = float.MinValue;

            foreach (MCTSNode child in node.ChildNodes)
            {
                UCTValue = child.Q / child.N + Math.Sqrt(Math.Log(child.Parent.N) / node.N);

                if (UCTValue > MaxUCTValue)
                {
                    bestChild.Clear();
                    bestChild.Add(child);
                    MaxUCTValue = UCTValue;
                }
                else if (UCTValue == MaxUCTValue)
                {
                    bestChild.Add(child);
                }
            }
            if (bestChild.Count == 1)
                return bestChild[0];
            else if (bestChild.Count == 0)
                return null;
            else
                return bestChild[RandomGenerator.Next(0, bestChild.Count)];
        }


        protected GOB.Action BestFinalAction(MCTSNode node)
        {
            //TODO: implement
            throw new NotImplementedException();
        }

    }
}
