using System;
using System.Collections.Generic;
using System.Timers;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;
using TaleWorlds.TwoDimension;

namespace RealmsForgotten.Career.Ability
{
    public class ClassAbility
    {
        public enum AbilityType
        {
            TroopBuff
        }
        // timer
        private int _coolDownLeft = 0;
        private readonly Timer _timer;
        private float _cooldown_end_time;
        private float _durationEndTime = 0;
        private readonly bool _isLocked;
        private readonly int cooldown;
        private readonly int duration;

        // data
        private string sprite;
        private string spriteUpgraded;
        private bool isUpgraded;
        public bool IsActive 
        {
            get
            {
                return _durationEndTime > Mission.Current.CurrentTime;
            }
        }
        public string Sprite
        {
            get
            {
                return isUpgraded? spriteUpgraded : sprite;
            }
        }
        public TextObject Name { get; private set; }
        Action? activateEffect;
        public delegate void OnTroopHitDelegate(Agent attacker, Agent victim, ref float[] additionalDamagePercentages, ref float[] resistancePercentages);
        public OnTroopHitDelegate? onTroopHit;
        public int GetCoolDownLeft() => _coolDownLeft;
        public ClassAbility(TextObject _name, string _sprite, string _spriteUpgraded, int _duration, int _coolDown, Action? _activateEffect = null, OnTroopHitDelegate? _onTroopHit = null)
        {
            Name = _name;
            sprite = _sprite;
            spriteUpgraded = _spriteUpgraded;
            isUpgraded = false;
            _timer = new Timer(1000);
            _timer.Elapsed += TimerElapsed;
            _timer.Enabled = false;
            onTroopHit = _onTroopHit;
            activateEffect = _activateEffect;
            duration = _duration;
            cooldown = _coolDown;
        }
        private void TimerElapsed(object sender, ElapsedEventArgs e)
        {
            if (Mission.Current == null)
            {
                FinalizeTimer();
                return;
            }

            _coolDownLeft = (int)(_cooldown_end_time - Mission.Current.CurrentTime);
            if (_coolDownLeft <= 0)
            {
                FinalizeTimer();
            }
        }
        private void FinalizeTimer()
        {
            _coolDownLeft = 0;
            _timer.Stop();
        }
        public bool IsDisabled(Agent casterAgent, out TextObject disabledReason)
        {
            disabledReason = new TextObject("{=!}Enabled");
            if (IsOnCooldown())
            {
                disabledReason = new TextObject("{=!}On cooldown");
                return true;
            }
            if (_isLocked)
            {
                disabledReason = new TextObject("{=!}Mission is over");
                return true;
            }
            return false;
        }
        public void SetCoolDown()
        {
            _coolDownLeft = cooldown;
            _durationEndTime = Mission.Current.CurrentTime + duration; 
            _cooldown_end_time = Mission.Current.CurrentTime + _coolDownLeft + 0.8f; //Adjustment was needed for natural tick on UI
            _timer.Start();
        }
        public bool IsOnCooldown() => _timer.Enabled;
        public virtual bool CanActivate(Agent casterAgent, out TextObject failureReason)
        {
            if (IsDisabled(casterAgent, out failureReason))
            {
                return false;
            }
            if (!casterAgent.IsActive() || casterAgent.Health <= 0)
            {
                failureReason = new TextObject("Caster is dead or routed");
                return false;
            }
            failureReason = null;
            return true;
        }

        public bool TryActivate(Agent casterAgent, out TextObject failureReason)
        {
            if (CanActivate(casterAgent, out failureReason))
            {
                Activate(casterAgent);
                failureReason = null;
                return true;
            }
            return false;
        }
        protected virtual void Activate(Agent casterAgent)
        {
            //OnCastStart?.Invoke(this);
            activateEffect?.Invoke();
            SetCoolDown();
        }
    }
}
