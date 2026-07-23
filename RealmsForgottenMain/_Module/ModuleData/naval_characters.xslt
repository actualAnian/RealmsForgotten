<?xml version="1.0" encoding="utf-8"?>
<xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform">
  <xsl:output omit-xml-declaration="yes"/>

  <!-- Identity transform: copy everything as-is by default. -->
  <xsl:template match="@*|node()">
    <xsl:copy>
      <xsl:apply-templates select="@*|node()"/>
    </xsl:copy>
  </xsl:template>

  <!-- Re-theme the WarSails naval characters to the RF cultures. WarSails ships
       naval variants (convoy crews, militia, and the Nord seafarer tree) still
       carrying vanilla culture names. We rewrite the display name to the RF name
       used by that culture's land troops, and keep only non-character entries
       (templates / practice dummies) hidden from the encyclopedia.

       Prefix map (mirrors RF's own caravan/militia troop names, nothing invented):
         Nord      -> Northmen   (+ borrowed Old-Norse role words normalised)
         Imperial  -> North Realm
         Sturgian  -> Dreadrealm
         Vlandian  -> Nasorian
         Battanian -> Elvean
         Aserai    -> Athas
         Khuzait   -> AllKhuur

       The rewritten name uses the {=!} literal marker so no leftover localisation
       key can pull the old vanilla text back in. Pirate cultures (sea_raiders,
       southern_pirates) and neutral crew are left untouched by design. -->
  <xsl:template match="NPCCharacter[@culture='Culture.nord']     |
                        NPCCharacter[@culture='Culture.empire']   |
                        NPCCharacter[@culture='Culture.sturgia']  |
                        NPCCharacter[@culture='Culture.vlandia']  |
                        NPCCharacter[@culture='Culture.battania'] |
                        NPCCharacter[@culture='Culture.aserai']   |
                        NPCCharacter[@culture='Culture.khuzait']">
    <xsl:copy>
      <!-- Copy every attribute except the two we manage below. -->
      <xsl:apply-templates select="@*[name()!='name' and name()!='is_hidden_encyclopedia']"/>

      <!-- Name: compute the RF name; rewrite (as a {=!} literal) only if it changed,
           otherwise leave the original attribute — key and all — untouched. -->
      <xsl:variable name="orig">
        <xsl:choose>
          <xsl:when test="contains(@name, '}')">
            <xsl:value-of select="substring-after(@name, '}')"/>
          </xsl:when>
          <xsl:otherwise>
            <xsl:value-of select="@name"/>
          </xsl:otherwise>
        </xsl:choose>
      </xsl:variable>
      <xsl:variable name="new">
        <xsl:call-template name="rf-rename">
          <xsl:with-param name="s" select="$orig"/>
          <xsl:with-param name="culture" select="substring-after(@culture, 'Culture.')"/>
        </xsl:call-template>
      </xsl:variable>
      <xsl:choose>
        <xsl:when test="@name and $new != $orig">
          <xsl:attribute name="name">
            <xsl:text>{=!}</xsl:text>
            <xsl:value-of select="$new"/>
          </xsl:attribute>
        </xsl:when>
        <xsl:when test="@name">
          <xsl:attribute name="name">
            <xsl:value-of select="@name"/>
          </xsl:attribute>
        </xsl:when>
      </xsl:choose>

      <!-- Visibility: hide only templates / practice dummies; real characters keep
           whatever the DLC set (so the renamed troops show up again). -->
      <xsl:choose>
        <xsl:when test="contains(@id, 'template') or contains(@id, 'practice') or contains(@id, 'dummy')">
          <xsl:attribute name="is_hidden_encyclopedia">true</xsl:attribute>
        </xsl:when>
        <xsl:otherwise>
          <xsl:apply-templates select="@is_hidden_encyclopedia"/>
        </xsl:otherwise>
      </xsl:choose>

      <xsl:apply-templates select="node()"/>
    </xsl:copy>
  </xsl:template>

  <!-- Culture-scoped name rewrite. The Nord branch also normalises the borrowed
       Old-Norse role words before swapping the prefix. -->
  <xsl:template name="rf-rename">
    <xsl:param name="s"/>
    <xsl:param name="culture"/>
    <xsl:choose>
      <xsl:when test="$culture = 'nord'">
        <xsl:variable name="n1"><xsl:call-template name="str-replace"><xsl:with-param name="t" select="$s"/><xsl:with-param name="from" select="'Shield Biters'"/><xsl:with-param name="to" select="'Shieldbearer'"/></xsl:call-template></xsl:variable>
        <xsl:variable name="n2"><xsl:call-template name="str-replace"><xsl:with-param name="t" select="$n1"/><xsl:with-param name="from" select="'Sky-Gods Chosen'"/><xsl:with-param name="to" select="'Chosen'"/></xsl:call-template></xsl:variable>
        <xsl:variable name="n3"><xsl:call-template name="str-replace"><xsl:with-param name="t" select="$n2"/><xsl:with-param name="from" select="'Berserkir'"/><xsl:with-param name="to" select="'Berserker'"/></xsl:call-template></xsl:variable>
        <xsl:variable name="n4"><xsl:call-template name="str-replace"><xsl:with-param name="t" select="$n3"/><xsl:with-param name="from" select="'Huscarl'"/><xsl:with-param name="to" select="'Guardsman'"/></xsl:call-template></xsl:variable>
        <xsl:variable name="n5"><xsl:call-template name="str-replace"><xsl:with-param name="t" select="$n4"/><xsl:with-param name="from" select="'Skjaldbrestir'"/><xsl:with-param name="to" select="'Shieldbreaker'"/></xsl:call-template></xsl:variable>
        <xsl:variable name="n6"><xsl:call-template name="str-replace"><xsl:with-param name="t" select="$n5"/><xsl:with-param name="from" select="'Ulfhedinn'"/><xsl:with-param name="to" select="'Wolf-Warrior'"/></xsl:call-template></xsl:variable>
        <xsl:variable name="n7"><xsl:call-template name="str-replace"><xsl:with-param name="t" select="$n6"/><xsl:with-param name="from" select="'Nord '"/><xsl:with-param name="to" select="'Northmen '"/></xsl:call-template></xsl:variable>
        <xsl:call-template name="str-replace"><xsl:with-param name="t" select="$n7"/><xsl:with-param name="from" select="'Vlandian '"/><xsl:with-param name="to" select="'Northmen '"/></xsl:call-template>
      </xsl:when>
      <xsl:when test="$culture = 'empire'">
        <xsl:call-template name="str-replace"><xsl:with-param name="t" select="$s"/><xsl:with-param name="from" select="'Imperial '"/><xsl:with-param name="to" select="'North Realm '"/></xsl:call-template>
      </xsl:when>
      <xsl:when test="$culture = 'sturgia'">
        <xsl:call-template name="str-replace"><xsl:with-param name="t" select="$s"/><xsl:with-param name="from" select="'Sturgian '"/><xsl:with-param name="to" select="'Dreadrealm '"/></xsl:call-template>
      </xsl:when>
      <xsl:when test="$culture = 'vlandia'">
        <xsl:call-template name="str-replace"><xsl:with-param name="t" select="$s"/><xsl:with-param name="from" select="'Vlandian '"/><xsl:with-param name="to" select="'Nasorian '"/></xsl:call-template>
      </xsl:when>
      <xsl:when test="$culture = 'battania'">
        <xsl:call-template name="str-replace"><xsl:with-param name="t" select="$s"/><xsl:with-param name="from" select="'Battanian '"/><xsl:with-param name="to" select="'Elvean '"/></xsl:call-template>
      </xsl:when>
      <xsl:when test="$culture = 'aserai'">
        <xsl:call-template name="str-replace"><xsl:with-param name="t" select="$s"/><xsl:with-param name="from" select="'Aserai '"/><xsl:with-param name="to" select="'Athas '"/></xsl:call-template>
      </xsl:when>
      <xsl:when test="$culture = 'khuzait'">
        <xsl:call-template name="str-replace"><xsl:with-param name="t" select="$s"/><xsl:with-param name="from" select="'Khuzait '"/><xsl:with-param name="to" select="'AllKhuur '"/></xsl:call-template>
      </xsl:when>
      <xsl:otherwise>
        <xsl:value-of select="$s"/>
      </xsl:otherwise>
    </xsl:choose>
  </xsl:template>

  <!-- Replace every occurrence of $from with $to inside $t (XSLT 1.0). -->
  <xsl:template name="str-replace">
    <xsl:param name="t"/>
    <xsl:param name="from"/>
    <xsl:param name="to"/>
    <xsl:choose>
      <xsl:when test="$from != '' and contains($t, $from)">
        <xsl:value-of select="substring-before($t, $from)"/>
        <xsl:value-of select="$to"/>
        <xsl:call-template name="str-replace">
          <xsl:with-param name="t" select="substring-after($t, $from)"/>
          <xsl:with-param name="from" select="$from"/>
          <xsl:with-param name="to" select="$to"/>
        </xsl:call-template>
      </xsl:when>
      <xsl:otherwise>
        <xsl:value-of select="$t"/>
      </xsl:otherwise>
    </xsl:choose>
  </xsl:template>

</xsl:stylesheet>
