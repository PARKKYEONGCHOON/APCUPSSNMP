using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using SnmpSharpNet; //Nuget 설치

namespace VisionSystem
{
    public class SNMP
    {

        public const string strSystemUptimeOid = "1.3.6.1.2.1.1.3.0";
        // 배터리 상태를 나타냅니다. 이 값은 UPS의 배터리 상태 1: Unknown, 2: Battery Normal, 3: Battery Low, 4: Battery In Fault Condition
        public const string strBatteryStatus = "1.3.6.1.4.1.318.1.1.1.2.2.1";
        //UPS가 배터리로 전환된 경우, 남은 배터리 시간을 초 단위로 반환합니다. 이 값은 **초(second)**로 표현
        public const string strBatteryRuntimeRemaining = "1.3.6.1.4.1.318.1.1.1.2.2.3";
        //배터리의 현재 전압을 나타냅니다. 이는 Volt 단위로 표현됩니다.
        public const string strBatteryVoltage = "1.3.6.1.4.1.318.1.1.1.2.2.2";
        //배터리 용량을 **백분율(%)**로 나타냅니다. 0에서 100 사이의 값으로 배터리의 현재 충전 상태를 표현
        public const string strBatteryCapacity = "1.3.6.1.4.1.318.1.1.1.2.2.4";
        //배터리 교체가 필요한지를 나타냅니다. 값은 다음과 같이 해석 1: No Battery Needs Replacement, 2: Battery Needs Replacement
        public const string strReplaceBatteryStatus = "1.3.6.1.4.1.318.1.1.1.2.2.12";
        //배터리의 현재 온도를 **섭씨(°C)**로 나타냅니다.
        public const string strBatteryTemperature = "1.3.6.1.4.1.318.1.1.1.2.2.11";

        public List<string> lst_batteryOid = new List<string>
        {

            strBatteryStatus,
            strBatteryRuntimeRemaining,
            strBatteryVoltage,
            strBatteryCapacity,
            strReplaceBatteryStatus,
            strBatteryTemperature

        };

        //UPS가 현재 Bypass Mode에 있는지 확인 1: 활성화X 2: 활성화O
        public const string strUPSBypassStatus = "1.3.6.1.4.1.318.1.1.1.4.3.6.0";
        //UPS의 Bypass Mode를 제어하는 데 사용됩니다. 이 OID를 통해 UPS를 Bypass 모드로 전환하거나 해제할 수 있습니다. 1: 변경없음, 2: bypassOn, 3: bypassOff
        public const string strUPSBypassModeControl = "1.3.6.1.4.1.318.1.1.1.4.2.3.0";

        public string snmpAgent;  // SNMP 장치 IP 주소
        public string community;         // SNMP 커뮤니티 문자열

        public IpAddress agentIp = null;
        public AgentParameters agenparam = null;

        private readonly object lockObject = new object(); // 동기화 객체
        public SNMP()
        {
            snmpAgent = "192.168.1.100";
            community = "public";
            agentIp = new IpAddress(snmpAgent);

            SetSNMPVersion();
        }


        public SNMP(string Agent, string commu)
        {
            snmpAgent = Agent;
            community = commu;
            agentIp = new IpAddress(snmpAgent);

            SetSNMPVersion();
        }

        public void SetSNMPVersion()
        {
            try
            {
                OctetString communityString = new OctetString(community);
                agenparam = new AgentParameters(communityString);
                agenparam.Version = SnmpVersion.Ver2;
            }
            catch
            {
                
            }           
           
        }

        public List<string> GetRequestV1V2(List<string> oids)
        {
            try
            {

                List<string> Response = new List<string>();

                lock (lockObject) // GET 요청에 대해 lock을 사용하여 동기화
                {
                    using (UdpTarget target = new UdpTarget((IPAddress)agentIp, 161, 2000, 1))
                    {
                        Pdu pdu = new Pdu(PduType.Get);

                        foreach (var tmpoid in oids)
                        {
                            Oid oid = new Oid(tmpoid);
                            pdu.VbList.Add(oid);
                        }

                        // SNMP GET 요청 전송
                        SnmpV2Packet result = (SnmpV2Packet)target.Request(pdu, agenparam);

                        // 결과 처리
                        if (result != null && result.Pdu.ErrorStatus == 0)
                        {
                            foreach (var res in result.Pdu.VbList)
                            {
                                Response.Add(res.ToString());
                            }
                        }
                    }
                }

                return Response;
            }
            catch
            {
                return new List<string>();
            }
        }

        public bool SetByPassMode(int mode)
        {
            bool Response = false;

            try
            {
                lock (lockObject) // SET 요청에 대해서도 lock을 사용하여 동기화
                {
                    using (UdpTarget target = new UdpTarget((IPAddress)agentIp, 161, 2000, 1))
                    {
                        Pdu pdu = new Pdu(PduType.Set);

                        Oid oid = new Oid(strUPSBypassModeControl);
                        pdu.VbList.Add(oid, new Integer32(mode));

                        // SNMP SET 요청 전송
                        SnmpV2Packet result = (SnmpV2Packet)target.Request(pdu, agenparam);

                        // 결과 처리
                        if (result != null && result.Pdu.ErrorStatus == 0)
                        {
                            Response = true;
                        }
                    }
                }
            }
            catch
            {
                Response = false;
            }

            return Response;
        }

        



    }
}
