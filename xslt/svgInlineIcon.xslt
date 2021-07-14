<?xml version="1.0" encoding="utf-8" ?>
<xsl:stylesheet
	version="1.0"
	xmlns:xsl="http://www.w3.org/1999/XSL/Transform"
	xmlns:svg="http://www.w3.org/2000/svg"
	exclude-result-prefixes="svg"
>
	<xsl:output method="xml" indent="no" omit-xml-declaration="yes" />

	<xsl:param name="iconsFile" />
	<xsl:param name="iconName" />
	
	<xsl:template match="/">
		<xsl:apply-templates select="svg:svg/svg:symbol[@id = $iconName]" />
	</xsl:template>
	
	<xsl:template match="svg:*">
		<xsl:element name="{local-name()}">
			<xsl:copy-of select="@*" />
			<xsl:apply-templates />
		</xsl:element>
	</xsl:template>
	
	<xsl:template match="svg:symbol" priority="1">
		<svg>
			<xsl:copy-of select="@*" />
			<xsl:apply-templates />
		</svg>
	</xsl:template>
	
</xsl:stylesheet>
