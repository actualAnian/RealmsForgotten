<xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform">
  <xsl:output omit-xml-declaration="yes" />
  <xsl:template match="@*|node()">
    <xsl:copy>
      <xsl:apply-templates select="@*|node()" />
    </xsl:copy>
  </xsl:template>

  <xsl:template match="action_set[not(@base_set) and @skeleton='human_skeleton' and @movement_system='bipedal' and action[@type='act_troop_cavalry_sword']]">
    <xsl:copy>
      <xsl:copy-of select="@*" />
      <xsl:copy-of select="*" />
      <action type="act_dual_ready_thrust_1h" animation="ready_dual_thrust_1h" />
      <action type="act_dual_quick_release_thrust_1h" animation="quick_release_dual_thrust_1h" />
      <action type="act_dual_quick_release_thrust_1h_balanced" animation="quick_release_dual_thrust_1h_balanced" />
      <action type="act_dual_release_thrust_1h" animation="release_dual_thrust_1h" />
      <action type="act_dual_release_thrust_1h_balanced" animation="release_dual_thrust_1h_balanced" />
      <action type="act_dual_quick_blocked_thrust_1h" animation="quick_dual_blocked_thrust_1h" />
      <action type="act_dual_quick_blocked_thrust_1h_balanced" animation="quick_dual_blocked_thrust_1h_balanced" />
      <action type="act_dual_blocked_thrust_1h" animation="blocked_thrust_1h" />
      <action type="act_dual_blocked_thrust_1h_balanced" animation="blocked_thrust_1h_balanced" />
      <action type="act_dual_stuck_thrust_1h" animation="stuck_thrust_1h" />
      <action type="act_dual_stuck_quick_thrust_1h" animation="stuck_quick_thrust_1h" />
      <action type="act_dual_ready_thrust_1h_left_stance" animation="ready_thrust_1h_left_stance" />
      <action type="act_dual_quick_release_thrust_1h_left_stance" animation="quick_release_thrust_1h_left_stance" />
      <action type="act_dual_quick_release_thrust_1h_balanced_left_stance" animation="quick_release_thrust_1h_balanced_left_stance" />
      <action type="act_dual_release_thrust_1h_left_stance" animation="release_thrust_1h_left_stance" />
      <action type="act_dual_release_thrust_1h_balanced_left_stance" animation="release_thrust_1h_balanced_left_stance" />
      <action type="act_dual_quick_blocked_thrust_1h_left_stance" animation="quick_blocked_thrust_1h_left_stance" />
      <action type="act_dual_quick_blocked_thrust_1h_balanced_left_stance" animation="quick_blocked_thrust_1h_balanced_left_stance" />
      <action type="act_dual_blocked_thrust_1h_left_stance" animation="blocked_thrust_1h_left_stance" />
      <action type="act_dual_blocked_thrust_1h_balanced_left_stance" animation="blocked_thrust_1h_balanced_left_stance" />
      <action type="act_dual_stuck_thrust_1h_left_stance" animation="stuck_thrust_1h_left_stance" />
      <action type="act_dual_stuck_quick_thrust_1h_left_stance" animation="stuck_quick_thrust_1h_left_stance" />
      <action type="act_dual_ready_slashright_1h" animation="ready_slashright_1h" />
      <action type="act_dual_ready_from_up_slashright_1h" animation="ready_from_up_slashright_1h" />
      <action type="act_dual_ready_from_up_slashright_1h_unbalanced" animation="ready_from_up_slashright_1h_unbalanced" />
      <action type="act_dual_ready_from_right_slashright_1h" animation="ready_from_right_slashright_1h" />
      <action type="act_dual_ready_from_right_slashright_1h_unbalanced" animation="ready_from_right_slashright_1h_unbalanced" />
      <action type="act_dual_quick_release_slashright_1h" animation="quick_release_dual_slashright_1h" />
      <action type="act_dual_quick_release_slashright_1h_balanced" animation="quick_release_dual_slashright_1h_balanced" />
      <action type="act_dual_release_slashright_1h" animation="release_slashright_1h" />
      <action type="act_dual_release_slashright_1h_balanced" animation="release_slashright_1h_balanced" />
      <action type="act_dual_quick_blocked_slashright_1h" animation="quick_blocked_slashright_1h" />
      <action type="act_dual_quick_blocked_slashright_1h_balanced" animation="quick_blocked_slashright_1h_balanced" />
      <action type="act_dual_blocked_slashright_1h" animation="quick_dual_blocked_slashright_1h" />
      <action type="act_dual_blocked_slashright_1h_balanced" animation="quick_dual_blocked_slashright_1h_balanced" />
      <action type="act_dual_ready_slashright_1h_left_stance" animation="ready_slashright_1h_left_stance" />
      <action type="act_dual_ready_from_up_slashright_1h_left_stance" animation="ready_from_up_slashright_1h_left_stance" />
      <action type="act_dual_ready_from_up_slashright_1h_unbalanced_left_stance" animation="ready_from_up_slashright_1h_left_stance_unbalanced" />
      <action type="act_dual_ready_from_right_slashright_1h_left_stance" animation="ready_from_right_slashright_1h_left_stance" />
      <action type="act_dual_ready_from_right_slashright_1h_unbalanced_left_stance" animation="ready_from_right_slashright_1h_left_stance_unbalanced" />
      <action type="act_dual_quick_release_slashright_1h_left_stance" animation="quick_release_slashright_1h_left_stance" />
      <action type="act_dual_quick_release_slashright_1h_balanced_left_stance" animation="quick_release_slashright_1h_balanced_left_stance" />
      <action type="act_dual_release_slashright_1h_left_stance" animation="release_slashright_1h_left_stance" />
      <action type="act_dual_release_slashright_1h_balanced_left_stance" animation="release_slashright_1h_balanced_left_stance" />
      <action type="act_dual_quick_blocked_slashright_1h_left_stance" animation="quick_blocked_slashright_1h_left_stance" />
      <action type="act_dual_quick_blocked_slashright_1h_balanced_left_stance" animation="quick_blocked_slashright_1h_balanced_left_stance" />
      <action type="act_dual_blocked_slashright_1h_left_stance" animation="blocked_slashright_1h_left_stance" />
      <action type="act_dual_blocked_slashright_1h_balanced_left_stance" animation="blocked_slashright_1h_balanced_left_stance" />
      <action type="act_dual_ready_slashleft_1h" animation="ready_dual_slashleft_1h" />
      <action type="act_dual_ready_from_up_slashleft_1h" animation="ready_dual_slashleft_1h" />
      <action type="act_dual_ready_from_up_slashleft_1h_unbalanced" animation="ready_dual_slashleft_1h" />
      <action type="act_dual_ready_from_left_slashleft_1h" animation="ready_from_left_slashleft_1h" />
      <action type="act_dual_ready_from_left_slashleft_1h_unbalanced" animation="ready_from_left_slashleft_1h_unbalanced" />
      <action type="act_dual_quick_release_slashleft_1h" animation="quick_release_dual_slashleft_1h" />
      <action type="act_dual_quick_release_slashleft_1h_balanced" animation="quick_release_dual_slashleft_1h_balanced" />
      <action type="act_dual_release_slashleft_1h" animation="release_slashleft_1h" />
      <action type="act_dual_release_slashleft_1h_balanced" animation="release_slashleft_1h_balanced" />
      <action type="act_dual_quick_blocked_slashleft_1h" animation="quick_dual_blocked_slashleft_1h" />
      <action type="act_dual_quick_blocked_slashleft_1h_balanced" animation="quick_dual_blocked_slashleft_1h_balanced" />
      <action type="act_dual_blocked_slashleft_1h" animation="blocked_slashleft_1h" />
      <action type="act_dual_blocked_slashleft_1h_balanced" animation="blocked_slashleft_1h_balanced" />
      <action type="act_dual_ready_slashleft_1h_left_stance" animation="ready_slashleft_1h_left_stance" />
      <action type="act_dual_ready_from_up_slashleft_1h_left_stance" animation="ready_from_up_slashleft_1h_left_stance" />
      <action type="act_dual_ready_from_up_slashleft_1h_unbalanced_left_stance" animation="ready_from_up_slashleft_1h_left_stance_unbalanced" />
      <action type="act_dual_ready_from_left_slashleft_1h_left_stance" animation="ready_from_left_slashleft_1h_left_stance" />
      <action type="act_dual_ready_from_left_slashleft_1h_unbalanced_left_stance" animation="ready_from_left_slashleft_1h_left_stance_unbalanced" />
      <action type="act_dual_quick_release_slashleft_1h_left_stance" animation="quick_release_slashleft_1h_left_stance" />
      <action type="act_dual_quick_release_slashleft_1h_balanced_left_stance" animation="quick_release_slashleft_1h_balanced_left_stance" />
      <action type="act_dual_release_slashleft_1h_left_stance" animation="release_slashleft_1h_left_stance" />
      <action type="act_dual_release_slashleft_1h_balanced_left_stance" animation="release_slashleft_1h_balanced_left_stance" />
      <action type="act_dual_quick_blocked_slashleft_1h_left_stance" animation="quick_blocked_slashleft_1h_left_stance" />
      <action type="act_dual_quick_blocked_slashleft_1h_balanced_left_stance" animation="quick_blocked_slashleft_1h_balanced_left_stance" />
      <action type="act_dual_blocked_slashleft_1h_left_stance" animation="blocked_slashleft_1h_left_stance" />
      <action type="act_dual_blocked_slashleft_1h_balanced_left_stance" animation="blocked_slashleft_1h_balanced_left_stance" />
      <action type="act_walk_idle_1h_with_d_shld" animation="dual_stand_1h" />
      <action type="act_walk_forward_1h_with_d_shld" animation="dual_walk_forward_1h" />
      <action type="act_walk_idle_1h_with_d_shld_left_stance" animation="dual_stand_1h_left_stance" />
      <action type="act_walk_forward_1h_with_d_shld_left_stance" animation="dual_walk_forward_1h_left_stance" />
      <action type="act_run_idle_1h_with_d_shld" animation="dual_stand_1h" />
      <action type="act_run_forward_1h_with_d_shld" animation="dual_run_forward_1h_with_hand_shield" />
      <action type="act_run_idle_1h_with_d_shld_left_stance" animation="dual_stand_1h_left_stance" />
      <action type="act_run_forward_1h_with_d_shld_left_stance" animation="dual_run_forward_1h_with_hand_shield_left_stance" />
    </xsl:copy>
  </xsl:template>
</xsl:stylesheet>
