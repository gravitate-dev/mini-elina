using Sirenix.OdinInspector;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerSpellLibrary : MonoBehaviour
{
    public const int SPELL_SUMMON_SEX_SUCCUBUS = 1;
    public class Spell
    {
        public string name;
        public int SPELL_NUMBER;
        public float cooldown;
        public KeyCode hotkey;
        public ButtonCooldown buttonCooldown;
        public bool isOnCooldown()
        {
            return buttonCooldown.IsOnCooldown();
        }
        public Spell(int SPELL_NUMBER, string name, float cooldown, KeyCode hotkey, ButtonCooldown buttonCooldown)
        {
            this.SPELL_NUMBER = SPELL_NUMBER;
            this.name = name;
            this.cooldown = cooldown;
            this.hotkey = hotkey;
            this.buttonCooldown = buttonCooldown;
        }
    }
    [Required]
    public ButtonCooldown summonSuccubusButton;
    private List<Spell> spells = new List<Spell>();
    private void Awake()
    {
        RefreshSpellsLockedStatus();
    }

    // useful when unlocking a new spell
    public void RefreshSpellsLockedStatus()
    {
        spells.Clear();
        if (isSummonSuccubusUnlocked())
        {
            spells.Add(getSummonSuccubusSpell());
        }
    }
    public List<Spell> GetSpells()
    {
        return spells;
    }

    public void OnCastSpell(Spell spell)
    {
        spell.buttonCooldown.BeginCooldown(spell.cooldown);
    }

    private bool isSummonSuccubusUnlocked()
    {
        return true;
    }

    private Spell getSummonSuccubusSpell()
    {
        return new Spell(SPELL_SUMMON_SEX_SUCCUBUS, "Summon Succubus", 2.5f, KeyCode.Q, summonSuccubusButton);
    }

}
