<?xml version="1.0" encoding="utf-8" ?>
<!--
	transformer.xslt
	
	Transforms the Web.config transform, substituting the constants into
	place in the process.
	
-->
<xsl:stylesheet
	version="1.0"
	xmlns:xsl="http://www.w3.org/1999/XSL/Transform"
	xmlns:str="http://exslt.org/strings"
	exclude-result-prefixes="str"
>
	<xsl:variable name="hostname" select="substring-after(//rule/conditions/add[@input = '{HTTP_HOST}']/@pattern, 'www.')" />
	
	<!-- Identity transform -->
	<xsl:template match="* | text()">
		<xsl:copy>
			<xsl:copy-of select="@*" />
			<xsl:apply-templates select="* | text() | comment()" />
		</xsl:copy>
	</xsl:template>

	<!--
	Entities are not parsed or replaced in comments,
	but we'd actually like this specific one to be,
	so we're doing a manual replace.
	-->
	<xsl:template match="comment()">
		<xsl:comment>
			<xsl:value-of select="str:replace(., '&amp;hostname;', $hostname)" />
		</xsl:comment>
	</xsl:template>
	
	<!--
	The pattern string should (technically) have dots escaped,
	which is what we're doing here.
	-->
	<xsl:template match="rule/conditions/add[@input = '{HTTP_HOST}']">
		<add input="{@input}" pattern="{str:replace(@pattern, '.', '\.')}" />
	</xsl:template>

</xsl:stylesheet>
