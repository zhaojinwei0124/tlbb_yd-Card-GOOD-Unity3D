﻿using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using GCGame.Table;
using Games.CharacterLogic;

namespace Games.Battle
{
    public class SkillProjectile : SkillBase
    {
        private EffectBase m_AttackEffect;
        private EffectBase m_BehitEffect;
        private EffectBase m_ProjectileEffect;

        private bool m_IsAttackEffectEnd = true;

        public SkillProjectile(BattleCard owner, int id)
        {
            m_SkillOwner = owner;
            m_SkillId = id;
            m_SkillType = SkillType.E_SKILL_TYPE_PROJECTILE;
        }

        public override bool Init()
        {
            m_SkillTab = TableManager.GetSkillByID(m_SkillId);
            m_SkillDisplayTab = TableManager.GetSkillDisplayByID(m_SkillTab.Effect);
            m_BattleRoot = BattleUI.Instacne.battleRoot;

            return true;
        }

        public override bool LoadSkill()
        {
            m_AttackEffect = EffectManager.Instance.CreateEffect(this, (BattleEffectType)m_SkillDisplayTab.AttackAnimType, m_SkillDisplayTab.AttackAnim, m_SkillDisplayTab.AttackLib);

            if (!m_AttackEffect.Load())
            {
                Debug.LogError("Load attack effect error! " + m_SkillDisplayTab.AttackLib);
            }

            m_BehitEffect = EffectManager.Instance.CreateEffect(this, (BattleEffectType)m_SkillDisplayTab.BehitAnimType, m_SkillDisplayTab.BehitAnim, m_SkillDisplayTab.BehitLib);

            if (!m_BehitEffect.Load())
            {
                Debug.LogError("Load behit effect error! " + m_SkillDisplayTab.BehitLib);
            }

            m_ProjectileEffect = EffectManager.Instance.CreateEffect(this, (BattleEffectType)m_SkillDisplayTab.ProjectileAnimType, m_SkillDisplayTab.ProjectileAnim, m_SkillDisplayTab.ProjectileLib);

            if (!m_ProjectileEffect.Load())
            {
                Debug.LogError("Load behit effect error! " + m_SkillDisplayTab.ProjectileLib);
            }
            return true;
        }

        public override void UseSkill()
        {
            PlayAttackEffect();
        }

        //攻击动画-
        private void PlayAttackEffect()
        {
            //处理斗转星移
            if (!HandleSkill_DZXY(1, m_SkillOwner))
            {
                return;
            }

            m_IsAttackEffectEnd = false;

            EffectBase effect = m_AttackEffect.Duplicate();
            effect.EffectEnd += AttackEffectEndEventHandler;
            effect.EffectUpdate += AttackEffectUpdateEventHandler;
            effect.StartPos = effect.EndPos = m_SkillOwner.BattleCardObj.transform;
            effect.MoveSpeed = 0f;
            effect.DirectionPoint = CalculateEffectDirection();

            effect.Play(m_SkillOwner);

            //播放攻击音效
            AudioManager.Instance.PlayEffectSound(m_SkillDisplayTab.Music, Obj_MyselfPlayer.GetMe().acceleration);
            if (Obj_MyselfPlayer.GetMe().isGuideBattle)
            {
                ShowSkillName();
            }
        }

        private void AttackEffectEndEventHandler(EffectBase effect, BattleCard target, float total_time)
        {
            effect.EffectEnd -= AttackEffectEndEventHandler;
            EffectManager.Instance.DestroyEffect(effect);

            if (!m_IsAttackEffectEnd)
            {
                PlayProjectileEffect();
            }
            m_IsAttackEffectEnd = true;
            EventManager.Instance.Fire(EventDefine.BATTLE_CARD_ATTACK_END, m_SkillOwner.BattleCardObj);
        }

        private void AttackEffectUpdateEventHandler(EffectBase effect, BattleCard target, float total_time, float cur_time)
        {
            effect.EffectUpdate -= AttackEffectUpdateEventHandler;

            m_IsAttackEffectEnd = true;

            PlayProjectileEffect();
        }

        //子弹动画-
        private void PlayProjectileEffect()
        {
            float ftime = m_SkillDisplayTab.FlyTime * 0.001f;  //除以1000
            if (ftime < 0)
            {
                Debug.LogWarning("ftime = " + ftime);
                ftime = 0.5f;
            }

            //处理斗转星移
            if (!HandleSkill_DZXY(2, null))
            {
                return;
            }

            foreach (BattleCard battle_card in m_TargetList)
            {
                EffectBase effect = m_ProjectileEffect.Duplicate();
                effect.EffectEnd += ProjectileEffectEndEventHandler;
                effect.StartPos = m_SkillOwner.BattleCardObj.transform;
                effect.EndPos = battle_card.BattleCardObj.transform;
                effect.MoveSpeed = ftime;
                effect.DirectionPoint = battle_card.BattleCardObj.transform.localPosition;

                battle_card.AnimateBeHit(m_BehitMap[battle_card]);
                battle_card.ShowHitValue(m_BehitMap[battle_card], this, false);

                effect.Play(battle_card);//播放最后调用
            }
        }

        private void ProjectileEffectEndEventHandler(EffectBase effect, BattleCard target, float total_time)
        {
            effect.EffectEnd -= ProjectileEffectEndEventHandler;
            EffectManager.Instance.DestroyEffect(effect);

            PlayBehitEffect(target);
        }

        //受击动画-
        private void PlayBehitEffect(BattleCard target)
        {
            //处理斗转星移
            if (!HandleSkill_DZXY(3, target))
            {
                return;
            }

            EffectBase behit_effect = m_BehitEffect.Duplicate();
            behit_effect.EffectEnd += BehitEffectEndEventHandler;
            behit_effect.StartPos = behit_effect.EndPos = target.BattleCardObj.transform;
            behit_effect.MoveSpeed = 0f;
            behit_effect.DirectionPoint = target.BattleCardObj.transform.localPosition;//CalculateEffectDirection();

            target.AnimateBeHit(m_BehitMap[target]);
            target.ShowHitValue(m_BehitMap[target], this, false);

            behit_effect.Play(target);//播放最后调用
        }

        private void BehitEffectEndEventHandler(EffectBase effect, BattleCard target, float total_time)
        {
            effect.EffectEnd -= BehitEffectEndEventHandler;
            EffectManager.Instance.DestroyEffect(effect);

            target.OnHit(m_SkillOwner, m_BehitMap[target]);
        }

        private Vector3 CalculateEffectDirection()
        {
            //计算目标的方向, 1.找到目标的中间点--
            List<Vector3> pos_list = new List<Vector3>();
            for (int i = 0; i < m_TargetList.Count; i++)
            {
                pos_list.Add(m_TargetList[i].BattleCardObj.transform.localPosition);
            }
            pos_list.Sort(delegate(Vector3 v1, Vector3 v2) { return Comparer<float>.Default.Compare(v1.x, v2.x); });
            Vector3 mid, left, right;
            left = pos_list[0];
            right = pos_list[pos_list.Count - 1];
            mid = Vector3.zero;
            mid.x = Math.Abs((right.x + left.x) * 0.5f);
            mid.y = Math.Abs((right.y + left.y) * 0.5f);

            return mid;
        }

        //处理斗转星移，如果卡牌使用斗转星移技能，目标是自己，给自己加斗转的Buff，走正常逻辑，读表，没有子弹和受击动画；
        //如果卡牌使用斗转星移技能，目标是别人，则使用特殊的表现，走非正常逻辑,bullet_dzxy,behit_dzxy
        //正常逻辑返回true，非正常逻辑返回false
        private bool HandleSkill_DZXY(int skill_step, BattleCard target)
        {
            switch (skill_step)
            {
                case 1:
                    if ((m_SkillTab.Effect == 50024 || m_SkillTab.Effect == 60024) && m_TargetList.Count > 0 && m_TargetList[0] != m_SkillOwner)
                    {
                        PlayProjectileEffect();
                        return false;
                    }
                    break;
                case 2:
                    if ((m_SkillTab.Effect == 50024 || m_SkillTab.Effect == 60024) && m_TargetList.Count > 0 && m_TargetList[0] != m_SkillOwner)
                    {
                        foreach (BattleCard battle_card in m_TargetList)
                        {
                            EffectBase effect = EffectManager.Instance.CreateEffect(this, BattleEffectType.E_BATTLE_EFFECT_TYPE_PARTICLE, "bullet_dzxy", "");
                            effect.Load();
                            effect.EffectEnd += ProjectileEffectEndEventHandler;
                            effect.StartPos = m_SkillOwner.BattleCardObj.transform;
                            effect.EndPos = battle_card.BattleCardObj.transform;
                            effect.MoveSpeed = 0.5f;
                            effect.DirectionPoint = battle_card.BattleCardObj.transform.localPosition;

                            effect.Play(battle_card);//播放最后调用
                        }
                        return false;
                    }
 
                    break;
                case 3:
                    if ((m_SkillTab.Effect == 50024 || m_SkillTab.Effect == 60024) && m_TargetList.Count > 0 && m_TargetList[0] != m_SkillOwner)
                    {
                        EffectBase behit_effect = EffectManager.Instance.CreateEffect(this, BattleEffectType.E_BATTLE_EFFECT_TYPE_PARTICLE, "behit_dzxy", "");
                        behit_effect.Load();
						behit_effect.EffectEnd += BehitEffectEndEventHandler;
                        behit_effect.StartPos = behit_effect.EndPos = target.BattleCardObj.transform;
                        behit_effect.MoveSpeed = 0f;
                        behit_effect.DirectionPoint = CalculateEffectDirection();

                        target.AnimateBeHit(m_BehitMap[target]);
                        target.ShowHitValue(m_BehitMap[target], this, false);

                        behit_effect.Play(target);//播放最后调用
                        return false;
                    }
                    break;
            }

            return true;
        }
    }
}
