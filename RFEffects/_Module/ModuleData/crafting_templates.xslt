<xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform">
	<xsl:output omit-xml-declaration="yes"/>
	<xsl:template match="@*|node()">
		<xsl:copy>
			<xsl:apply-templates select="@*|node()"/>
		</xsl:copy>
	</xsl:template>

	<xsl:template match="CraftingTemplate[@id='TwoHandedSword']/UsablePieces/UsablePiece[1]">
		<UsablePiece piece_id="vlandian_blade_8_fire"/>
		<UsablePiece piece_id="vlandian_blade_2_fire"/>
		<UsablePiece piece_id="vlandian_blade_3_fire"/>
		<xsl:copy>
			<xsl:apply-templates select="@*|node()"/>
		</xsl:copy>
	</xsl:template>
</xsl:stylesheet>