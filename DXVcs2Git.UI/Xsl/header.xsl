<?xml version="1.0"?>
<xsl:stylesheet
    xmlns:xsl="http://www.w3.org/1999/XSL/Transform" version="1.0">

    <xsl:output method="html"/>

    <xsl:template match="/">
        <xsl:variable name="modification.list" select="/cruisecontrol/modifications/modification"/>
        <xsl:variable name="link.list" select="//link"/>

        <table class="section-table" cellpadding="2" cellspacing="0" border="0" width="1300">

            <xsl:if test="/cruisecontrol/exception">
                <tr><td class="header-title" colspan="2">BUILD EXCEPTION</td></tr>
                <tr>
                    <td class="header-label" valign="top"><nobr>Error Message:</nobr></td>
                    <td class="header-data-error"><xsl:value-of select="/cruisecontrol/exception"/></td>
                </tr>
            </xsl:if>
            
            <xsl:if test="/cruisecontrol/build/@error">
                <tr><td class="header-title" colspan="2">BUILD FAILED</td></tr>
            </xsl:if>
            <xsl:if test="not (/cruisecontrol/build/@error) and not (/cruisecontrol/exception)">
                <tr><td class="header-title" colspan="2">BUILD SUCCESSFUL</td></tr>
            </xsl:if>

			<tr>
                <td class="header-label"><nobr>Project:</nobr></td>
                <td class="header-data"><xsl:value-of select="/cruisecontrol/@project"/></td>
			</tr>
            <tr>
                <td class="header-label"><nobr>Date of build:</nobr></td>
                <td class="header-data"><xsl:value-of select="/cruisecontrol/build/@date"/></td>
            </tr>
            <tr>
                <td class="header-label"><nobr>Running time:</nobr></td>
                <td class="header-data"><xsl:value-of select="/cruisecontrol/build/@buildtime"/></td>
            </tr>
            <tr>
                <td class="header-label"><nobr>Integration Request:</nobr></td>
                <td class="header-data">
					<xsl:value-of select="/cruisecontrol/request" />
                </td>
            </tr>
          <xsl:if test="/cruisecontrol/build/@error">
            <tr>
              <td class="header-label" >
                <nobr>Project message:</nobr>
              </td>
              <td class="header-data">
                <xsl:value-of select="/cruisecontrol/build/ProjectMessage" />
              </td>
            </tr>
          </xsl:if>

          <tr>
                <td class="header-label"><nobr>Build On:</nobr></td>
                <td class="header-data">
					<xsl:value-of select="/cruisecontrol/build/remoteServer" />
                </td>
            </tr>
            <tr>
                <td class="header-label"><nobr>Building Path:</nobr></td>
                <td class="header-data">
                    <a>
					<xsl:attribute name="href">file:<xsl:value-of select="translate(/cruisecontrol/build/remotePath, '\', '/')" /></xsl:attribute>
					<xsl:value-of select="/cruisecontrol/build/remotePath" />
					</a>
                </td>
            </tr>
            <tr>
                <td class="header-label"><nobr>Build max working set:</nobr></td>
                <td class="header-data">
					<xsl:number value="/cruisecontrol/build/maxRemoteWorkingSet" grouping-separator="," grouping-size="3" /> bytes
                </td>
            </tr>
            <tr>
                <td class="header-label"><nobr>Pending Time:</nobr></td>
                <td class="header-data">
					<xsl:value-of select="/cruisecontrol/build/pendingTime" />
                </td>
            </tr>

            <xsl:if test="count($link.list) > 0">
                <xsl:for-each  select="$link.list">
                    <tr>
                        <td class="header-label">
                            <nobr>
                                <xsl:value-of select="@label" />
                            </nobr>
                        </td>
                        <td class="header-data">
                            <a target="_blank" >
                                <xsl:attribute name="href">
                                    <xsl:value-of select="@href" />
                                </xsl:attribute>

                                <xsl:value-of select="current()" />
                            </a>
                        </td>
                    </tr>
                </xsl:for-each>
            </xsl:if>

            <xsl:apply-templates select="$modification.list">
                <xsl:sort select="date" order="descending" data-type="text" />
            </xsl:apply-templates>
            
        </table>
    </xsl:template>

    <!-- Last Modification template -->
    <xsl:template match="/cruisecontrol/modifications/modification">
        <xsl:if test="position() = 1">
            <tr>
                <td class="header-label"><nobr>Last changed:</nobr></td>
                <td class="header-data"><xsl:value-of select="date"/></td>
            </tr>
            <tr>
                <td class="header-label" valign="top"><nobr>Last log entry:</nobr></td>
                <td class="header-data"><pre><xsl:value-of select="comment"/></pre></td>
            </tr>
        </xsl:if>
    </xsl:template>
</xsl:stylesheet>
