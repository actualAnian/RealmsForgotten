<?xml version="1.0" encoding="utf-8"?>
<xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform" xmlns="">
  <xsl:output omit-xml-declaration="no" indent="yes" />
  <xsl:template match="@*|node()">
    <xsl:copy>
      <xsl:apply-templates select="@*|node()" />
    </xsl:copy>
  </xsl:template>

  <!-- RBM compatibility: enchanted robes/capes and monster hide keep their look and
       base-game values, but count as Chainmail under RBM's procedural armor curve so
       boss mages and undead are not made of paper. Applies only when RBM_RF is active. -->
  <xsl:template match="Item[@id='red_priest_elite_mage_outfit' or @id='necromancer_boss_robe' or @id='mummy_body' or @id='allkhuur_goddess_cape' or @id='grand_mage_cape' or @id='mage_leader_cape']/ItemComponent/Armor/@material_type">
    <xsl:attribute name="material_type">Chainmail</xsl:attribute>
  </xsl:template>
</xsl:stylesheet>
