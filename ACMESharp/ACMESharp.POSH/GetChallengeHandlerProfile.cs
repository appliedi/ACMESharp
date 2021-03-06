﻿using ACMESharp.ACME;
using ACMESharp.Util;
using ACMESharp.Vault.Profile;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using System.Text;
using System.Threading.Tasks;

namespace ACMESharp.POSH
{
    [Cmdlet(VerbsCommon.Get, "ChallengeHandlerProfile", DefaultParameterSetName = PSET_GET_HANDLER_PROFILE)]
    public class GetChallengeHandlerProfile : Cmdlet
    {
        public const string PSET_GET_CHALLENGE_TYPES = "GetChallengeTypes";
        public const string PSET_GET_CHALLENGE_HANDLERS = "GetChallengeHandlers";
        public const string PSET_LIST_HANDLER_PROFILES = "ListHandlerProfiles";
        public const string PSET_GET_HANDLER_PROFILE = "GetHandlerProfile";

        [Parameter(ParameterSetName = PSET_GET_CHALLENGE_TYPES)]
        public SwitchParameter ListChallengeTypes
        { get; set; }

        [Parameter(ParameterSetName = PSET_GET_CHALLENGE_TYPES)]
        public string GetChallengeType
        { get; set; }

        [Parameter(ParameterSetName = PSET_GET_CHALLENGE_HANDLERS)]
        public SwitchParameter ListChallengeHandlers
        { get; set; }

        [Parameter(ParameterSetName = PSET_GET_CHALLENGE_HANDLERS)]
        public string GetChallengeHandler
        { get; set; }

        [Parameter(ParameterSetName = PSET_GET_CHALLENGE_HANDLERS)]
        public SwitchParameter ParametersOnly
        { get; set; }

        [Parameter(Mandatory = true, ParameterSetName = PSET_LIST_HANDLER_PROFILES)]
        public SwitchParameter ListProfiles
        { get; set; }

        [Parameter(Mandatory = true, ParameterSetName = PSET_GET_HANDLER_PROFILE)]
        public string ProfileRef
        { get; set; }

        [Parameter(ParameterSetName = PSET_LIST_HANDLER_PROFILES)]
        [Parameter(ParameterSetName = PSET_GET_HANDLER_PROFILE)]
        public string VaultProfile
        { get; set; }

        protected override void ProcessRecord()
        {
            // We have to invoke this here because we *may not* invoke
            // any Vault access but we do rely on Ext mechanism access.
            Util.PoshHelper.BeforeExtAccess();

            if (!string.IsNullOrEmpty(GetChallengeType))
            {
                WriteVerbose("Getting details of Challenge Type Decoder");
                var tInfo = ChallengeDecoderExtManager.GetProviders()
                        .FirstOrDefault(_ => _.Name == GetChallengeType);
                var t = ChallengeDecoderExtManager.GetProvider(GetChallengeType);
                WriteObject(new {
                        tInfo.Name,
                        tInfo.Info.Label,
                        tInfo.Info.SupportedType,
                        ChallengeType = tInfo.Info.Type,
                        tInfo.Info.Description,
                    });
            }
            else if (ListChallengeTypes)
            {
                WriteVerbose("Listing all Challenge Type Decoders");
                WriteObject(ChallengeDecoderExtManager.GetProviders().Select(_ => _.Name), true);
            }
            else if (!string.IsNullOrEmpty(GetChallengeHandler))
            {
                WriteVerbose("Getting details of Challenge Type Handler");
                var pInfo = ChallengeHandlerExtManager.GetProviders()
                        .FirstOrDefault(_ => _.Name == GetChallengeHandler);
                var p = ChallengeHandlerExtManager.GetProvider(GetChallengeHandler);
                if (ParametersOnly)
                {
                    WriteVerbose("Showing parameter details only");
                    WriteObject(p.DescribeParameters().Select(_ => new {
                            _.Name,
                            _.Label,
                            _.Type,
                            _.IsRequired,
                            _.IsMultiValued,
                            _.Description,
                        }), true);
                }
                else
                {
                    WriteObject(new {
                            pInfo.Name,
                            pInfo.Info.Label,
                            pInfo.Info.SupportedTypes,
                            pInfo.Info.Description,
                            Parameters = p.DescribeParameters().Select(_ => new {
                                    _.Name,
                                    _.Label,
                                    _.Type,
                                    _.IsRequired,
                                    _.IsMultiValued,
                                    _.Description,
                                }),
                        });
                }
            }
            else if (ListChallengeHandlers)
            {
                WriteVerbose("Listing all Challenge Type Handlers");
                WriteObject(ChallengeHandlerExtManager.GetProviders().Select(_ => _.Name), true);
            }
            else
            {
                WriteVerbose("Getting details of preconfigured Challenge Handler Profile");
                using (var vlt = Util.VaultHelper.GetVault(VaultProfile))
                {
                    vlt.OpenStorage();
                    var v = vlt.LoadVault();

                    if (ListProfiles)
                    {
                        WriteObject(v.ProviderProfiles?.Values, true);
                    }
                    else
                    {
                        var ppi = v.ProviderProfiles?.GetByRef(ProfileRef, false);
                        if (ppi == null)
                        {
                            WriteObject(ppi);
                        }
                        else
                        {
                            var asset = vlt.GetAsset(Vault.VaultAssetType.ProviderConfigInfo,
                                    ppi.Id.ToString());
                            using (var s = vlt.LoadAsset(asset))
                            {
                                WriteObject(JsonHelper.Load<ProviderProfile>(s), false);
                            }
                        }
                    }
                }
            }
        }
    }
}
