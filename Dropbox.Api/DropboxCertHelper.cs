﻿namespace Dropbox.Api
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;

    /// <summary>
    /// Helper methods that can be used to implement certificate pinning.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Dropbox recommends that all clients implement certficate pinning, unfortunately it isn't currently
    /// possible to implement this in a portable assembly, so this class is provided to help implement this.</para>
    /// <para>
    /// For more information about certificate pinning see
    /// <a href="https://www.owasp.org/index.php/Certificate_and_Public_Key_Pinning">Certificate and Public Key Pinning</a>.
    /// </para>
    /// <para>
    /// These helper methods allow client code to check if the certificate used by a Dropbox server
    /// was issued with a certificate chain that originates with a root certificate that Dropbox
    /// either currently uses, or may use in the future.
    /// </para>
    /// </remarks>
    /// <example>
    /// <para>The following code demonstrates how to implement certificate pinning on a desktop or
    /// server application.</para>
    /// <code>
    /// private void InitializeCertPinning()
    /// {
    ///     ServicePointManager.ServerCertificateValidationCallback = (sender, certificate, chain, sslPolicyErrors) =&gt;
    ///     {
    ///         var root = chain.ChainElements[chain.ChainElements.Count - 1];
    ///         var publicKey = root.Certificate.GetPublicKeyString();
    ///
    ///         return DropboxCertHelper.IsKnownRootCertPublicKey(publicKey);
    ///     };
    /// }
    /// </code>
    /// <para>This code would be called before calling the <see cref="DropboxClient"/> constructor.</para>
    /// <para><strong>Note:</strong> If your application is communicating with other web services you may need
    /// to perform different pinning checks for different services.</para>
    /// </example>
    public static class DropboxCertHelper
    {
        /// <summary>
        /// The public keys of the known valid root certificates
        /// </summary>
        private static readonly HashSet<string> ValidRoots = new HashSet<string> { 
            // CN=DigiCert Assured ID Root CA, OU=www.digicert.com, O=DigiCert Inc, C=US
            "3082010A0282010100AD0E15CEE443805CB187F3B760F97112A5AEDC269488AAF4CEF520" +
            "392858600CF880DAA9159532613CB5B128848A8ADC9F0A0C83177A8F90AC8AE779535C31" +
            "842AF60F98323676CCDEDD3CA8A2EF6AFB21F25261DF9F20D71FE2B1D9FE1864D2125B5F" +
            "F9581835BC47CDA136F96B7FD4B0383EC11BC38C33D9D82F18FE280FB3A783D6C36E44C0" +
            "61359616FE599C8B766DD7F1A24B0D2BFF0B72DA9E60D08E9035C678558720A1CFE56D0A" +
            "C8497C3198336C22E987D0325AA2BA138211ED39179D993A72A1E6FAA4D9D5173175AE85" +
            "7D22AE3F014686F62879C8B1DAE45717C47E1C0EB0B492A656B3BDB297EDAAA7F0B7C5A8" +
            "3F9516D0FFA196EB085F18774F0203010001",
            // CN=DigiCert Global Root CA, OU=www.digicert.com, O=DigiCert Inc, C=US
            "3082010A0282010100E23BE11172DEA8A4D3A357AA50A28F0B7790C9A2A5EE12CE965B01" +
            "0920CC0193A74E30B753F743C46900579DE28D22DD870640008109CECE1B83BFDFCD3B71" +
            "46E2D666C705B37627168F7B9E1E957DEEB748A308DAD6AF7A0C3906657F4A5D1FBC17F8" +
            "ABBEEE28D7747F7A78995985686E5C23324BBF4EC0E85A6DE370BF7710BFFC01F685D9A8" +
            "44105832A97518D5D1A2BE47E2276AF49A33F84908608BD45FB43A84BFA1AA4A4C7D3ECF" +
            "4F5F6C765EA04B37919EDC22E66DCE141A8E6ACBFECDB3146417C75B299E32BFF2EEFAD3" +
            "0B42D4ABB74132DA0CD4EFF881D5BB8D583FB51BE84928A270DA3104DDF7B216F24C0A4E" +
            "07A8ED4A3D5EB57FA390C3AF270203010001",
            // CN=DigiCert High Assurance EV Root CA, OU=www.digicert.com, O=DigiCert Inc, C=US
            "3082010A0282010100C6CCE573E6FBD4BBE52D2D32A6DFE5813FC9CD2549B6712AC3D594" +
            "3467A20A1CB05F69A640B1C4B7B28FD098A4A941593AD3DC94D63CDB7438A44ACC4D2582" +
            "F74AA5531238EEF3496D71917E63B6ABA65FC3A484F84F6251BEF8C5ECDB3892E306E508" +
            "910CC4284155FBCB5A89157E71E835BF4D72093DBE3A38505B77311B8DB3C724459AA7AC" +
            "6D00145A04B7BA13EB510A984141224E656187814150A6795C89DE194A57D52EE65D1C53" +
            "2C7E98CD1A0616A46873D03404135CA171D35A7C55DB5E64E13787305604E511B4298012" +
            "F1793988A202117C2766B788B778F2CA0AA838AB0A64C2BF665D9584C1A1251E875D1A50" +
            "0B2012CC41BB6E0B5138B84BCB0203010001",
            // CN=Entrust Root Certification Authority - EC1, OU="(c) 2012 Entrust, Inc. - for authorized use only", OU=See www.entrust.net/legal-terms, O="Entrust, Inc.", C=US
            "048413C9D0BA6D417BE26CD0EB555F66021A24F45B896947E3B8C27DF1F202C59FA0F65B" +
            "D58B0619864F53106D072427A1A0F8D54719614C7DCA9327EA740CEF6F9609FE63EC705D" +
            "36AD6777AEC99D7C55443AA263511FF5E362D4A947073ECC20",
            // CN=Entrust Root Certification Authority - G2, OU="(c) 2009 Entrust, Inc. - for authorized use only", OU=See www.entrust.net/legal-terms, O="Entrust, Inc.", C=US
            "3082010A0282010100BA84B672DB9E0C6BE299E93001A776EA32B895411AC9DA614E5872" +
            "CFFEF68279BF7361060AA527D8B35FD3454E1C72D64E32F2728A0FF78319D06A80800045" +
            "1EB0C7E79ABF1257271CA3682F0A87BD6A6B0E5E65F31C77D5D4858D7021B4B332E78BA2" +
            "D5863902B1B8D247CEE4C949C43BA7DEFB547D57BEF0E86EC279B23A0B55E25098163213" +
            "5C2F7856C1C294B3F25AE4279A9F24D7C6ECD09B2582E3CCC2C445C58C977A066B2A119F" +
            "A90A6E483B6FDBD4111942F78F07BFF5535F9C3EF4172CE669AC4E324C6277EAB7E8E5BB" +
            "34BC198BAE9C51E7B77EB553B13322E56DCF703C1AFAE29B67B683F48DA5AF624C4DE058" +
            "AC64341203F8B68D946324A4710203010001",
            // CN=Entrust Root Certification Authority, OU="(c) 2006 Entrust, Inc.", OU=www.entrust.net/CPS is incorporated by reference, O="Entrust, Inc.", C=US
            "3082010A0282010100B695B64342FAC66D2A6F48DF944C395705EEC37911416836EDECFE" +
            "9A018FA13828FCF71046662E4D1E1AB11A4EC6D1C09588B0C9FF318B3303DBB7837B3E20" +
            "845EEDB25628A7F8E0B9407137C5CB470E972A68C022956215DB47D9F5D02BFF824BC9AD" +
            "3EDE4CDB9080503F098A8400EC300A3D18CDFBFD2A599A2395172C459E1F6E43796D0C5C" +
            "98FE48A7C523475C5EFD6EE71EB4F66845D186835BA28A8DB1E32980FE257188ADBEBC8F" +
            "AC52964BAA518DE4133119E84E4D9FDBACB36AD5BC395471CA7A7A7F90DD7D1D80D981BB" +
            "5926C211FEE693E2F780E465FB34370E2980704DAF38862E9E7F57AF9E17AEEB1CCB2821" +
            "5FB61CD8E7A20422F9D3DAD8CB0203010001",
            // CN=Entrust.net Certification Authority (2048), OU=(c) 1999 Entrust.net Limited, OU=www.entrust.net/CPS_2048 incorp. by ref. (limits liab.), O=Entrust.net
            "3082010A0282010100AD4D4BA91286B2EAA320071516642A2B4BD1BF0B4A4D8EED8076A5" +
            "67B77840C07342C868C0DB532BDD5EB8769835938B1A9D7C133A0E1F5BB71ECFE524141E" +
            "B181A98D7DB8CC6B4B03F1020CDCABA54024007F7494A19D0829B3880BF587779D55CDE4" +
            "C37ED76A64AB851486955B9732506F3DC8BA660CE3FCBDB849C176894919FDC0A8BD89A3" +
            "672FC69FBC711960B82DE92CC99076667B94E2AF78D665535D3CD69CB2CF2903F92FA450" +
            "B2D448CE0532558AFDB2644C0EE4980775DB7FDFB9085560853029F97B48A46986E3353F" +
            "1E865D7A7A15BDEF008E1522541700902693BC0E496891BFF847D39D9542C10E4DDF6F26" +
            "CFC3182162664370D6D5C007E10203010001",
            // CN=GeoTrust Global CA, O=GeoTrust Inc., C=US
            "3082010A0282010100DACC186330FDF417231A567E5BDF3C6C38E471B77891D4BCA1D84C" +
            "F8A843B603E94D21070888DA582F663929BD05788B9D38E805B76A7E71A4E6C460A6B0EF" +
            "80E489280F9E25D6ED83F3ADA691C798C9421835149DAD9846922E4FCAF18743C1169557" +
            "2D50EF892D807A57ADF2EE5F6BD2008DB914F8141535D9C046A37B72C891BFC9552BCDD0" +
            "973E9C2664CCDFCE831971CA4EE6D4D57BA919CD55DEC8ECD25E3853E55C4F8C2DFE5023" +
            "36FC66E6CB8EA4391900B7950239910B0EFE382ED11D059AF64D3E6F0F071DAF2C1E8F60" +
            "39E2FA36531339D45E262BDB3DA814BD32EB180328520471E5AB333DE138BB073684629C" +
            "79EA1630F45FC02BE8716BE4F90203010001",
            // CN=GeoTrust Primary Certification Authority - G2, OU=(c) 2007 GeoTrust Inc. - For authorized use only, O=GeoTrust Inc., C=US
            "0415B1E8FD031543E5ACEB87371162EFD28336527D45570B4A8D7B543B3A6E5F1502C050" +
            "A6CF252F7DCA48B8C750631C2A21087C9A36D80BFED126C55831302825F35D5DA3B8B6A5" +
            "B492ED6C2C9FEBDD4389A23C4B48911D50EC26DFD6602EBD21",
            // CN=GeoTrust Primary Certification Authority - G3, OU=(c) 2008 GeoTrust Inc. - For authorized use only, O=GeoTrust Inc., C=US
            "3082010A0282010100DCE25E62581D3357393233FAEBCB878CA7D44ADD0688EA648E3198" +
            "A538901E98CF2E632BF046BC44B289A1C0280C497021959F64C0A6931202652686C6A589" +
            "F0FAD784A070AF4F1A973F0644D5C9EB72107DE43128FB1C61E628074473922269A70388" +
            "6C9D63C852DA9827E7084C703EB4C912C1C567835D33F30311EC6AD053E2D1BA36609480" +
            "BB61636C5B177EDF40941EAB0DC221287088FFD6266C6C6004254E557E7DEFBF9448DEB7" +
            "1DDD708D055F88A59BF2C2EEEAD140416D62381D5606C50347512019FC7B100B0E62AE76" +
            "55BF5F77BE3E4901533D98250376245A1DB4DB89EA79E5B6B33B3FBA4C28417F06AC6A8E" +
            "C1D0F6051D7DE64286E3A5D5470203010001",
            // CN=GeoTrust Primary Certification Authority, O=GeoTrust Inc., C=US
            "3082010A0282010100BEB8157BFFD47C7D67AD83647BC842532DDFF684082061D601596A" +
            "9C4411AFEF76FD957ECE6130BB7A835F02BD0166CAEE158D6FA1309CBDA1859E943AF356" +
            "880031CFD8EE6A9602D9ED038CFB756DE7EAB8551605169AF4E05EB188C064855C154D88" +
            "C7B7BAE075E9AD053D9DC78948E0BB28C803E13093645E52C05970223557888AF1950A83" +
            "D7BC31730134EDEF4671E06B02A835726B979B66E0CB1C795FD81A04681E4702E69D60E2" +
            "369701DFCE3592DFBE67C76D77593B8F9DD6901594BC423410C139F9B1273E7ED68A75C5" +
            "B2AF96D3A2DE9BE498BE7DE1E981ADB66FFCD70EDAE034B00D1A77E7E30898EF58FA9C84" +
            "B736AFC2DFACD2F410067071350203010001",
            // OU=Go Daddy Class 2 Certification Authority, O="The Go Daddy Group, Inc.", C=US
            "308201080282010100DE9DD7EA571849A15BEBD75F4886EABEDDFFE4EF671CF46568B357" +
            "71A05E77BBED9B49E970803D561863086FDAF2CCD03F7F0254225410D8B281D4C0753D4B" +
            "7FC777C33E78AB1A03B5206B2F6A2BB1C5887EC4BB1EB0C1D845276FAA3758F78726D7D8" +
            "2DF6A917B71F72364EA6173F659892DB2A6E5DA2FE88E00BDE7FE58D15E1EBCB3AD5E212" +
            "A2132DD88EAF5F123DA0080508B65CA565380445991EA3606074C541A572621B62C51F6F" +
            "5F1A42BE025165A8AE23186AFC7803A94D7F80C3FAAB5AFCA140A4CA1916FEB2C8EF5E73" +
            "0DEE77BD9AF67998BCB10767A2150DDDA058C6447B0A3E62285FBA41075358CF117E3874" +
            "C5F8FFB569908F8474EA971BAF020103",
            // CN=Go Daddy Root Certificate Authority - G2, O="GoDaddy.com, Inc.", L=Scottsdale, S=Arizona, C=US
            "3082010A0282010100BF716208F1FA5934F71BC918A3F7804958E9228313A6C52043013B" +
            "84F1E685499F27EAF6841B4EA0B4DB7098C73201B1053E074EEEF4FA4F2F593022E7AB19" +
            "566BE28007FCF316758039517BE5F935B6744EA98D8213E4B63FA90383FAA2BE8A156A7F" +
            "DE0BC3B6191405CAEAC3A804943B467C320DF3006622C88D696D368C1118B7D3B21C60B4" +
            "38FA028CCED3DD4607DE0A3EEB5D7CC87CFBB02B53A4926269512505611A44818C2CA943" +
            "9623DFAC3A819A0E29C51CA9E95D1EB69E9E300A39CEF18880FB4B5DCC32EC8562432534" +
            "0256270191B43B702A3F6EB1E89C88017D9FD4F9DB536D609DBF2CE758ABB85F46FCCEC4" +
            "1B033C09EB49315C6946B3E0470203010001",
            // SERIALNUMBER=07969287, CN=Go Daddy Secure Certification Authority, OU=http://certificates.godaddy.com/repository, O="GoDaddy.com, Inc.", L=Scottsdale, S=Arizona, C=US
            "3082010A0282010100C42DD5158C9C264CEC3235EB5FB859015AA66181593B7063ABE3DC" +
            "3DC72AB8C933D379E43AED3C3023848EB33014B6B287C33D9554049EDF99DD0B251E21DE" +
            "65297E35A8A954EBF6F73239D4265595ADEFFBFE5886D79EF4008D8C2A0CBD4204CEA73F" +
            "04F6EE80F2AAEF52A16966DABE1AAD5DDA2C66EA1A6BBBE51A514A002F48C79875D8B929" +
            "C8EEF8666D0A9CB3F3FC787CA2F8A3F2B5C3F3B97A91C1A7E6252E9CA8ED12656E6AF612" +
            "4453703095C39C2B582B3D08744AF2BE51B0BF87D04C27586BB535C59DAF1731F80B8FEE" +
            "AD813605890898CF3AAF2587C049EAA7FD67F7458E97CC1439E23685B57E1A37FD16F671" +
            "119A743016FE1394A33F840D4F0203010001",
            // E=premium-server@thawte.com, CN=Thawte Premium Server CA, OU=Certification Services Division, O=Thawte Consulting cc, L=Cape Town, S=Western Cape, C=ZA
            "30818902818100D236366A8BD7C25B9EDA8141628F38EE490455D6D0EF1C1B951647EF18" +
            "48353A52F42B6A068F3B2FEA56E3AF868D9E17F79EB46575024DEFCB09A22151D89BD067" +
            "D0BA0D92061473D493CB972A009C5C4E0CBCFA1552FCF2446EDA114A6E089F2F2DE3F9AA" +
            "3A8673B6465358C88905BD8311B8733FAA078DF4424DE7409D1C370203010001",
            // CN=thawte Primary Root CA - G2, OU="(c) 2007 thawte, Inc. - For authorized use only", O="thawte, Inc.", C=US
            "04A2D59C827B959DF1527887FE8A16BF05E6DFA3024F0D07C60051BA0C02522D22A44239" +
            "C4FE8FEAC9C1BED44DFF9F7A9EE2B17C9AADA786097387D1E79AE37AA5AA6EFBBAB370C0" +
            "6788A235D4A39AB1FDADC2EF31FAA8B9F3FB08C691D1FB2995",
            // CN=thawte Primary Root CA - G3, OU="(c) 2008 thawte, Inc. - For authorized use only", OU=Certification Services Division, O="thawte, Inc.", C=US
            "3082010A0282010100B2BF272CFBDBD85BDD787B1B9E776681CB3EBC7CAEF3A6279A34A3" +
            "683171383362E4F3716679B1A965A3A58BD58F602D3F42CCAA6B32C023CB2C41DDE4DFFC" +
            "619CE273B222951143185FC4B61F576C0A055822C8364C3A7CA5D1CF86AF88A744021374" +
            "71730A425902F81B146B42DF6F5FBA6B82A29D5BE74ABD1E0172DB4B74E83B7F7F7D1F04" +
            "B4269BE0B45AAC473D55B8D7B026522801314066D8D924BDF62AD8EC21495C9BF67AE97F" +
            "55357E966B8D939327CB92BBEAAC40C09FC2F880CF5DF45ADCCE7486A63E6C0B53CABD92" +
            "CE190672E60C5C3869C704D6BC6CCE5BF6F7689CDC25154888A1E9A9F8989CE0F3D53128" +
            "61116C67968D3999CBC24524390203010001",
            // CN=thawte Primary Root CA, OU="(c) 2006 thawte, Inc. - For authorized use only", OU=Certification Services Division, O="thawte, Inc.", C=US
            "3082010A0282010100ACA0F0FB8059D49CC7A4CF9DA159730910450C0D2C6E68F16C5B48" +
            "68495937FC0B3319C2777FCC102D95341CE6EB4D09A71CD2B8C9973602B789D4245F06C0" +
            "CC4494948D02626FEB5ADD118D289A5C8490107A0DBD74662F6A38A0E2D55444EB1D079F" +
            "07BA6FEEE9FD4E0B29F53E84A001F19CABF81C7E89A4E8A1D871650DA3517BEEBCD22260" +
            "0DB95B9DDFBAFC515B0BAF98B2E92EE904E86287DE2BC8D74EC14C641EDDCF8758BA4A4F" +
            "CA68071D1C9D4AC6D52F91CC7C71721CC5C067EB32FDC9925C94DA85C09BBF537D2B09F4" +
            "8C9D911F976A52CBDE0936A477D87B875044D53E6E2969FB3949261E09A5807B402DEBE8" +
            "2785C9FE61FD7EE67C971DD59D0203010001",
        };

        /// <summary>
        /// Determines whether the specified public key string is a known root certificate public key.
        /// </summary>
        /// <param name="publicKeyString">The public key string.</param>
        /// <returns><c>true</c> if the specified string is a known root certificate
        /// public key; <c>false</c> otherwise.</returns>
        public static bool IsKnownRootCertPublicKey(string publicKeyString)
        {
            return ValidRoots.Contains(publicKeyString);
        }

        /// <summary>
        /// Determines whether the specified public key is a known root certificate public key.
        /// </summary>
        /// <param name="publicKey">The public key.</param>
        /// <returns><c>true</c> if the specified public key is a known root certificate
        /// public key; <c>false</c> otherwise.</returns>
        public static bool IsKnownRootCertPublicKey(byte[] publicKey)
        {
            var publicKeyString = BitConverter.ToString(publicKey).Replace("-", "");

            return ValidRoots.Contains(publicKeyString);
        }
    }
}
