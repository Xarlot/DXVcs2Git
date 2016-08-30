<?xml version="1.0"?>
<xsl:stylesheet
    xmlns:xsl="http://www.w3.org/1999/XSL/Transform" version="1.0">

    <xsl:output method="html"/>

        <xsl:variable name="cr"><xsl:text>&#xD;</xsl:text></xsl:variable>

        <xsl:template match="/">
        <xsl:if test="contains(., ': FAILED:')">
			<xsl:variable name="content">
				<xsl:for-each select="/cruisecontrol//buildresults//message">
					<xsl:value-of select="text()" /><xsl:value-of select="$cr" />
				</xsl:for-each>
			</xsl:variable>
			<table class="section-table" cellpadding="2" cellspacing="0" border="0" width="98%">

				<!-- Unit Tests -->
				<tr>
					<td class="sectionheader" colspan="2">Install errors:</td>
				</tr>
				<tr><td colspan="2" class="error">
					<xsl:call-template name="extract-errors">
						  <xsl:with-param name="word" select="$content"/>
						  <xsl:with-param name="mask" select="': FAILED:'"/>
						</xsl:call-template>
				</td></tr>
			</table>
        </xsl:if>
        <xsl:if test="contains(., ': warning ') and contains(.,'private working set')">
			<xsl:variable name="content">
				<xsl:for-each select="/cruisecontrol//buildresults//message">
					<xsl:value-of select="text()" /><xsl:value-of select="$cr" />
				</xsl:for-each>
			</xsl:variable>
			<table class="section-table" cellpadding="2" cellspacing="0" border="0" width="98%">

				<!-- Unit Tests -->
				<tr>
					<td class="sectionheader" colspan="2">Install warnings:</td>
				</tr>
				<tr><td colspan="2" class="warning">
					<xsl:call-template name="extract-warnings">
						  <xsl:with-param name="word" select="$content"/>
						  <xsl:with-param name="mask" select="'warning'"/>
						</xsl:call-template>
				</td></tr>
			</table>
        </xsl:if>
    </xsl:template>

    <xsl:template name="extract-errors">
        <xsl:param name="word"/>
        <xsl:param name="mask"/>
        <xsl:choose>
			<xsl:when test="contains($word,$cr)">
				<xsl:if test="contains(substring-before($word,$cr), $mask)">
					<xsl:value-of select="substring-before($word,$mask)"/>
					<xsl:value-of select="$cr"/>
					<p class="section-data">
						<xsl:variable name="errorText">
							<xsl:value-of select="substring-before(substring-after($word, $mask),'private working set')"/>
						</xsl:variable>
						<xsl:choose>
							<xsl:when test="contains($errorText,'data:image')">
								<xsl:call-template name="br-replace">
									<xsl:with-param name="word" select="substring-before(substring-after($word, $mask),'data:image')"/>
								</xsl:call-template>
								<xsl:text>See screenshot</xsl:text>
							</xsl:when>
							<xsl:otherwise>
								<xsl:call-template name="br-replace">
									<xsl:with-param name="word" select="$errorText"/>
								</xsl:call-template>
							</xsl:otherwise>
						</xsl:choose>
					</p>
					<br/>
				</xsl:if>
				<xsl:call-template name="extract-errors">
					<xsl:with-param name="word" select="substring-after(substring-after($word, 'private working set'), $cr)"/>
					<xsl:with-param name="mask" select="$mask"/>
				</xsl:call-template>
			</xsl:when>
            <xsl:otherwise>
				<xsl:if test="contains($word, $mask)">
					<xsl:value-of select="$word"/>
					<br/>
				</xsl:if>
            </xsl:otherwise>
        </xsl:choose>
    </xsl:template>

	<xsl:template name="extract-warnings">
		<xsl:param name="word"/>
		<xsl:param name="mask"/>
		<xsl:choose>
			<xsl:when test="contains($word,'private working set')">
				<xsl:if test="contains(substring-before($word,'private working set'), $mask) and not(contains(substring-before($word,'private working set'), 'FAILED:'))">
					<xsl:value-of select="substring-after(substring-before($word,'OK:'), $cr)"/>
					<xsl:value-of select="$cr"/>
					<p class="section-data">
						<xsl:call-template name="br-replace">
							<xsl:with-param name="word" select="substring-after(substring-before($word,'private working set'), 'OK:')"/>
						</xsl:call-template>
					</p>
					<br/>
				</xsl:if>
				<xsl:call-template name="extract-warnings">
					<xsl:with-param name="word" select="substring-after($word, 'private working set')"/>
					<xsl:with-param name="mask" select="$mask"/>
				</xsl:call-template>
			</xsl:when>
			<xsl:otherwise>
				<xsl:if test="contains($word, $mask)">
				<xsl:value-of select="$word"/>
				<br/>
				</xsl:if>
			</xsl:otherwise>
		</xsl:choose>
	</xsl:template>

	<xsl:template name="br-replace">
		<xsl:param name="word"/>
		<xsl:variable name="crD">
			<xsl:text>&#xD;</xsl:text>
		</xsl:variable>
		<xsl:variable name="crA">
			<xsl:text>&#xA;</xsl:text>
		</xsl:variable>
		<xsl:variable name="crAD">
			<xsl:text>
			</xsl:text>
		</xsl:variable>
		<xsl:choose>
			<xsl:when test="contains($word,$crAD)">
				<xsl:value-of select="substring-before($word,$crAD)"/>
				<br/>
				<xsl:call-template name="br-replace">
					<xsl:with-param name="word" select="substring-after($word,$crAD)"/>
				</xsl:call-template>
			</xsl:when>
			<xsl:when test="contains($word,$crA)">
				<xsl:value-of select="substring-before($word,$crA)"/>
				<br/>
				<xsl:call-template name="br-replace">
					<xsl:with-param name="word" select="substring-after($word,$crA)"/>
				</xsl:call-template>
			</xsl:when>
			<xsl:when test="contains($word,$crD)">
				<xsl:value-of select="substring-before($word,$crD)"/>
				<br/>
				<xsl:call-template name="br-replace">
					<xsl:with-param name="word" select="substring-after($word,$crD)"/>
				</xsl:call-template>
			</xsl:when>
			<xsl:otherwise>
				<xsl:value-of select="$word"/>
			</xsl:otherwise>
		</xsl:choose>
	</xsl:template>

</xsl:stylesheet>
