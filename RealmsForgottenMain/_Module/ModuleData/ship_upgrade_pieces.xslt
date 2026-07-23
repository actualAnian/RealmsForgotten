<?xml version="1.0" encoding="utf-8"?>
<xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform">
  <xsl:output omit-xml-declaration="yes"/>

  <!-- Identity transform: copy everything as-is by default. -->
  <xsl:template match="@*|node()">
    <xsl:copy>
      <xsl:apply-templates select="@*|node()"/>
    </xsl:copy>
  </xsl:template>

  <!-- Re-theme the WarSails ship decoration pieces to the RF cultures. Only the
       empire-themed pieces ship with a vanilla culture word in their display name
       ("Imperial Basic Pavilion", ...); rewrite the prefix to the RF empire name
       (North Realm), mirroring the troop rename. The new name uses the {=!} literal
       marker so no leftover localisation key can pull the old text back in. -->
  <xsl:template match="ShipUpgradePiece[contains(@name, 'Imperial ')]">
    <xsl:copy>
      <xsl:apply-templates select="@*[name()!='name']"/>
      <xsl:attribute name="name">
        <xsl:text>{=!}</xsl:text>
        <xsl:call-template name="str-replace">
          <xsl:with-param name="t">
            <xsl:choose>
              <xsl:when test="contains(@name, '}')">
                <xsl:value-of select="substring-after(@name, '}')"/>
              </xsl:when>
              <xsl:otherwise>
                <xsl:value-of select="@name"/>
              </xsl:otherwise>
            </xsl:choose>
          </xsl:with-param>
          <xsl:with-param name="from" select="'Imperial '"/>
          <xsl:with-param name="to" select="'North Realm '"/>
        </xsl:call-template>
      </xsl:attribute>
      <xsl:apply-templates select="node()"/>
    </xsl:copy>
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
