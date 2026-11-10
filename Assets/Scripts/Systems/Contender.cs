using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Unity.Mathematics;
using Unity.Collections;

public class Contender : MonoBehaviour
{

    [Serializable]
    public struct State
    {

        [Tooltip("Affects maximum stamina and character speed for actions like running. Slowly decays over time and is replenished with food or certain drinks.")]
        public int energy;

        [Tooltip("Affects stamina recovery. Provides bonus when high and debuff when low.")]
        public int hydration;

        [Tooltip("Resource for growing muscle and replenishing energy.")]
        public int food;

        [Tooltip("Resource for performing actions. Recovers when in a rested state.")]
        public int stamina;

        public static State operator +(State state, State mod) 
        {

            return new State() { energy = state.energy + mod.energy, hydration = state.hydration + mod.energy, food = state.food + mod.food, stamina = state.stamina + mod.stamina };

        }

        public static State operator -(State state, State mod)
        {

            return new State() { energy = state.energy - mod.energy, hydration = state.hydration - mod.energy, food = state.food - mod.food, stamina = state.stamina - mod.stamina };

        }

        public static implicit operator State(int4 values) => new State() { energy = values.x, hydration = values.y, food = values.z, stamina = values.w };
        public static implicit operator int4(State state) => new int4(state.energy, state.hydration, state.food, state.stamina);

    }

    [Serializable]
    public struct Modifier
    {

        public int duration;

        public State values;

    }

    [Serializable]
    public struct Physique
    {

        public int neck;

        public int trapsLeft;
        public int trapsRight;

        public int pecsLeft;
        public int pecsRight;

        public int deltsLeft;
        public int deltsRight;

        public int bicepsLeft;
        public int bicepsRight;

        public int tricepsLeft;
        public int tricepsRight;

        public int forearmsLeft;
        public int forearmsRight;

        public int upperBackLeft;
        public int upperBackRight;

        public int latsLeft;
        public int latsRight;

        public int lowerBackLeft;
        public int lowerBackRight;

        public int absALeft;
        public int absARight;

        public int absBLeft;
        public int absBRight;

        public int absCLeft;
        public int absCRight;

        public int absDLeft;
        public int absDRight;

        public int obliquesLeft;
        public int obliquesRight;

        public int pelvic;

        public int glutesLeft;
        public int glutesRight;

        public int quadsLeft;
        public int quadsRight;

        public int hamstringsLeft;
        public int hamstringsRight;

        public int calvesLeft;
        public int calvesRight;

    }

    protected State m_State;
    public State CurrentState => m_State;

    protected State m_Recovery;
    public State Recovery => m_Recovery;

    protected Physique m_Physique;
    public Physique CurrentPhysique => m_Physique;

    protected NativeList<Modifier> m_StateModifiers;
    protected NativeList<Modifier> m_RecoveryModifiers;

}
