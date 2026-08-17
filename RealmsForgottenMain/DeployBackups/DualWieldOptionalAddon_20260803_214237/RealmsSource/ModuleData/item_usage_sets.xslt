<xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform">
	<xsl:output omit-xml-declaration="yes" indent="yes" />

	<xsl:template match="node()|@*">
		<xsl:copy>
			<xsl:apply-templates select="node()|@*" />
		</xsl:copy>
	</xsl:template>


	<!-- I need to add a new Movement Set to Onehanded_Shield_Swing, BUT it must come before Onehanded_Swing_Cantblock or it will never be used -->

	<!-- Suppress the specific movement_set and add new movement_set above existing ones -->
	<xsl:template match="item_usage_set[@id='onehanded_shield_swing']/movement_sets">
		<xsl:copy>
			<!-- Add your new movement_set -->
			<movement_set id="1h_with_dual_shield" require_left_hand_usage_root_set="dual_shield" />

			<!-- Apply templates to all nodes except the one with id 'onehanded_swing_cantblock' -->
			<xsl:apply-templates select="node()[not(self::movement_set[@id='onehanded_swing_cantblock'])]" />

			<!-- Add back the suppressed movement_set -->
			<movement_set id="onehanded_swing_cantblock" />
		</xsl:copy>
	</xsl:template>








	<!-- I need to change the Require Free Left Hand to True, so that my new Usages will get called. I must additional Example of the Base-Game Usage back for cases where the Left Hand is not Free, but a Dual Weapon isn't used either -->

	<!-- Match the attribute require_free_left_hand of the first 'usage' node with style='attack_right' -->
	<xsl:template match="item_usage_set[@id='onehanded_shield_swing']/usages/usage[@style='attack_left'][1]/@require_free_left_hand">
		<xsl:attribute name="require_free_left_hand">True</xsl:attribute>
	</xsl:template>

	<!-- Match the attribute require_free_left_hand of the first 'usage' node with style='attack_right' -->
	<xsl:template match="item_usage_set[@id='onehanded_shield_swing_thrust']/usages/usage[@style='attack_down'][1]/@require_free_left_hand">
		<xsl:attribute name="require_free_left_hand">True</xsl:attribute>
	</xsl:template>
	

	<!-- Match the attribute item_usage_set and add a new usage' -->
	<xsl:template match="item_usage_set[@id='onehanded_shield_swing']/usages">
		<xsl:copy>
			<xsl:apply-templates select="@*|node()" />


			<usage style="attack_left"
				   ready_action="act_dual_ready_slashleft_1h"
				   ready_from_up_action="act_ready_from_up_slashleft_1h"
				   ready_from_left_action="act_ready_from_left_slashleft_1h"
				   quick_release_action="act_dual_quick_release_slashleft_1h"
				   release_action="act_release_slashleft_1h"
				   quick_blocked_action="act_dual_quick_blocked_slashleft_1h"
				   blocked_action="act_blocked_slashleft_1h"
				   is_mounted="False"
				   require_free_left_hand="False"
				   strike_type="swing"
				   begin_hand_position="0,0,0"
				   begin_hand_rotation="-90,90"
				   begin_arm_rotation="80,0"
				   begin_arm_length="0.5"
				   end_hand_position="0,0,-0.2"
				   end_hand_rotation="-90,-252"
				   end_arm_rotation="-108,0"
				   end_arm_length="0.5"
				   require_left_hand_usage_root_set="dual_shield" />




		</xsl:copy>
	</xsl:template>


	<!-- Match the attribute item_usage_set and add a new usage' -->
	<xsl:template match="item_usage_set[@id='onehanded_shield_swing_thrust']/usages">
		<xsl:copy>
			<xsl:apply-templates select="@*|node()" />
			<usage style="attack_down"
				   ready_action="act_dual_ready_thrust_1h"
				   quick_release_action="act_dual_quick_release_thrust_1h"
				   release_action="act_dual_release_thrust_1h"
				   quick_blocked_action="act_dual_quick_blocked_thrust_1h"
			 	   blocked_action="act_dual_blocked_thrust_1h"
				   quick_stuck_action="act_stuck_quick_thrust_1h"
				   stuck_action="act_stuck_thrust_1h"
				   is_mounted="False"
				   require_free_left_hand="False"
				   strike_type="thrust"
				   begin_hand_position="0,-0.4,-0.1"
				   begin_hand_rotation="0,-90"
				   begin_arm_rotation="0,0"
				   begin_arm_length="0"
				   end_hand_position="0,1,-0.1"
				   end_hand_rotation="0,-90"
				   end_arm_rotation="0,0"
				   end_arm_length="0"
				   require_left_hand_usage_root_set="dual_shield" />
			
			
		</xsl:copy>
	</xsl:template>
	
	
</xsl:stylesheet>
