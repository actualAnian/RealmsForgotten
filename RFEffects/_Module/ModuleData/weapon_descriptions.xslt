<xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform">
	<xsl:output omit-xml-declaration="yes"/>
	<xsl:template match="@*|node()">
		<xsl:copy>
			<xsl:apply-templates select="@*|node()"/>
		</xsl:copy>
	</xsl:template>

	<xsl:template match="WeaponDescription[@id='TwoHandedSword']/AvailablePieces/AvailablePiece[1]">
		<AvailablePiece id="vlandian_blade_8_fire"/>
		<AvailablePiece id="vlandian_blade_2_fire"/>
		<AvailablePiece id="vlandian_blade_3_fire"/>
		<xsl:copy>
			<xsl:apply-templates select="@*|node()"/>
		</xsl:copy>
	</xsl:template>
</xsl:stylesheet>