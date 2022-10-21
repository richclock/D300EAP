using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Net.Http;
using System.Net;
using System.Text.RegularExpressions;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;


using TCP;

namespace D300EAP.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class EAPController : ControllerBase
    {
        private static EAP _agv, _asrs, _cell;
        private string _carrier = "100001";
        private string _partNo = "SLM-YV358IP底模";
        //private string _partNo = "JH18093-R192492-201D";
        private string _operatorID = "A30114";
        private string _stationCode = "NCM";
        private int _machiningTime = 100;
        private static bool _jobRequest = false;

        [HttpGet]
        public IActionResult Get()
        {
            object response = new
            {
                ret = true
            };
            return StatusCode(StatusCodes.Status200OK, response);
        }


        // POST api/<controller>
        [HttpPost]
        [Route("post")]
        public IActionResult Post()
        {
            object response = new
            {
                ret = true
            };
            return StatusCode(StatusCodes.Status200OK, response);
        }

        [HttpPost]
        [Route("EAPOnline")]
        public async Task<IActionResult> EAPOnline()
        {
            //AGV ASRS
            object response = null;
            string value = null;
            using (StreamReader reader = new StreamReader(Request.Body, Encoding.UTF8))
            {
                value = await reader.ReadToEndAsync();
            }
            return await Task.Run(() =>
            {
                try
                {
                    _jobRequest = false;
                    dynamic cmd = JsonSerializer.Deserialize<dynamic>(value);
                    string equipment = cmd["Content"]["Equipment"];
                    string name = cmd["Content"]["Name"];
                    string ipAddress = cmd["Content"]["IPAddress"];
                    int port = int.Parse(cmd["Content"]["Port"]);
                    if (cmd["Content"]["Name"].ToString().ToUpper().Contains("AGV"))
                    {
                        try
                        {
                            _agv.Close();
                            _agv = null;
                        }
                        catch (Exception ex) { }
                        _agv = new EAP(equipment, name, ipAddress, port);
                    }
                    else if (cmd["Content"]["Name"].ToString().ToUpper().Contains("STOCK"))
                    {
                        try
                        {
                            _asrs.Close();
                            _asrs = null;
                        }
                        catch (Exception ex) { }
                        _asrs = new EAP(equipment, name, ipAddress, port);
                    }
                    response = new
                    {
                        Result = true,
                        Message = ""
                    };
                }
                catch (System.Exception ex)
                {
                    return StatusCode(StatusCodes.Status400BadRequest, null);
                }
                return StatusCode(StatusCodes.Status200OK, response);
            });

        }

        [HttpPost]
        [Route("JobRequest")]
        public async Task<IActionResult> JobRequest()
        {
            object response = null;
            string value = null;
            using (StreamReader reader = new StreamReader(Request.Body, Encoding.UTF8))
            {
                value = await reader.ReadToEndAsync();
            }
            return await Task.Run(() =>
            {
                dynamic cmd = JsonSerializer.Deserialize<dynamic>(value);
                if (!_jobRequest)
                {
                    try
                    {
                        _agv.Send(EAP.TransportRequestStock());
                        using (StreamWriter wr = new StreamWriter(System.AppDomain.CurrentDomain.SetupInformation.ApplicationBase + @"\EapLog.txt", true))
                        {
                            wr.WriteLine("AGV Send:");
                            wr.WriteLine("Time:" + DateTime.Now.ToString());
                            wr.WriteLine("Data:" + EAP.TransportRequestStock());
                            wr.WriteLine("-------------------------------------------------------");
                            wr.Flush();
                            wr.Close();
                        }
                    }
                    catch (Exception ex) { }
                    try
                    {
                        _asrs.Send(EAP.CarrierMoveoutRequest());
                        using (StreamWriter wr = new StreamWriter(System.AppDomain.CurrentDomain.SetupInformation.ApplicationBase + @"\EapLog.txt", true))
                        {
                            wr.WriteLine("ASRS Send:");
                            wr.WriteLine("Time:" + DateTime.Now.ToString());
                            wr.WriteLine("Data:" + EAP.CarrierMoveoutRequest());
                            wr.WriteLine("-------------------------------------------------------");
                            wr.Flush();
                            wr.Close();
                        }
                    }
                    catch (Exception ex) { }
                    _jobRequest = true;
                }


                response = new
                {
                    Result = true,
                    Message = ""
                };
                return StatusCode(StatusCodes.Status200OK, response);
            });
        }

        //進站開始
        [HttpPost]
        [Route("CarrierArrive")]
        public async Task<IActionResult> CarrierArrive()
        {
            object response = null;
            return await Task.Run(() =>
            {
                response = new
                {
                    Result = true,
                    Message = ""
                };
                return StatusCode(StatusCodes.Status200OK, response);
            });
        }

        [HttpPost]
        [Route("CheckAlive")]
        public async Task<IActionResult> CheckAlive()
        {
            object response = null;
            return await Task.Run(() =>
            {
                response = new
                {
                    Result = true,
                    Message = "",
                    ServerTime = ""
                };
                return StatusCode(StatusCodes.Status200OK, response);
            });
        }

        //進站結束+裝夾開始
        //收到後需使用socket傳送TransportRequest與CarrierMoveoutRequest
        [HttpPost]
        [Route("OperationVerify")]
        public async Task<IActionResult> OperationVerify()
        {
            object response = null;
            return await Task.Run(() =>
            {

                #region 取得加工程式
                string process = null;
                string ncFileName = "O0001";
                string ncContent = "M30";

                using (System.IO.StreamReader re = new System.IO.StreamReader(System.AppDomain.CurrentDomain.SetupInformation.ApplicationBase + @"\NC\O0001.NC"))
                {
                    ncContent = re.ReadToEnd();
                    ncContent = Base64.Encrypt(ncContent);
                }
                #endregion
                response = new
                {
                    StartSlotID = "",
                    EndSlotID = "",
                    TransactionID = "A201809051606499314000A",
                    CarrierCapacity = 1,
                    Lot = "1809020001|1",
                    LoadJaw = "JAW_JS_ML02",
                    LoadJawPort = "J02",
                    UnloadJaw = "JAW_JS_ML02",
                    UnloadJawPort = "",
                    Device = "JS",
                    ProcessNo = "1",
                    ComponentData = new object[] {
                new {
                    SID = "A2018090514514084730009",
                    ComponentID = "1809020001|1-01",
                    ComponentQty = 1.0,
                    Status = "WAIT",
                    SlotID = "1",
                    FAIFlag = false,
                    MeasureFlag = false,
                    CoordinateOriginFlag = false,
                    WaitResultFlag = true,
                    Tag = "-1"
                }},
                    RecipeData = new
                    {
                        SID = "",
                        ProcessProgram = new
                        {
                            Name = ncFileName,
                            Content = ncContent
                        },
                        MeasureProgram = new
                        {
                            Name = "",
                            Content = ""
                        },
                        CoordinateOriginProgram = new
                        {
                            Name = "",
                            Content = ""
                        },
                        ParameterDataList = new object[] { }
                    },
                    Result = true,
                    Message = ""
                };
                return StatusCode(StatusCodes.Status200OK, response);
            });

        }

        //裝夾結束+加工開始
        [HttpPost]
        [Route("OperationStart")]
        public async Task<IActionResult> OperationStart()
        {
            object response = null;
            return await Task.Run(() =>
            {

                response = new
                {
                    Result = true,
                    Message = ""
                };
                return StatusCode(StatusCodes.Status200OK, response);
            });
        }

        [HttpPost]
        [Route("EquipmentAddMainJawTool")]
        public async Task<IActionResult> EquipmentAddMainJawTool()
        {
            object response = null;
            return await Task.Run(() =>
            {
                response = new 
                {
                    Result = true,
                    Message = ""
                };
                return StatusCode(StatusCodes.Status200OK, response);
            });
        }

        [HttpPost]
        [Route("ProcessStart")]
        public async Task<IActionResult> ProcessStart()
        {
            object response = null;
            return await Task.Run(() =>
            {
                response = new
                {
                    Result = true,
                    Message = ""
                };
                return StatusCode(StatusCodes.Status200OK, response);
            });
        }

        [HttpPost]
        [Route("ProcessEnd")]
        public async Task<IActionResult> ProcessEnd()
        {
            object response = null;
            return await Task.Run(() =>
            {
                response = new
                {
                    Result = true,
                    Message = ""
                };
                return StatusCode(StatusCodes.Status200OK, response);
            });
        }

        //加工結束
        [HttpPost]
        [Route("OperationEnd")]
        public async Task<IActionResult> OperationEnd()
        {
            object response = null;
            return await Task.Run(() =>
            {
                response = new
                {
                    Result = true,
                    Message = ""
                };
                return StatusCode(StatusCodes.Status200OK, response);
            });
        }

        //出站開始
        [HttpPost]
        [Route("UnloadRequest")]
        public async Task<IActionResult> UnloadRequest()
        {
            object response = null;
            return await Task.Run(() =>
            {
                try
                {
                    _agv.UnloadRequest = true;
                    _agv.Send(EAP.TransportRequestML02());
                }
                catch (Exception ex) { }
                response = new
                {
                    Result = true,
                    Message = ""
                };
                return StatusCode(StatusCodes.Status200OK, response);
            });
        }

        //出站結束
        [HttpPost]
        [Route("CarrierLeave")]
        public async Task<IActionResult> CarrierLeave()
        {
            object response = null;
            return await Task.Run(() =>
            {

                response = new
                {
                    Result = true,
                    Message = ""
                };
                return StatusCode(StatusCodes.Status200OK, response);
            });
        }

        //AGV
        [HttpPost]
        [Route("TransportRequest")]
        public async Task<IActionResult> TransportRequest()
        {
            object response = null;
            return await Task.Run(() =>
            {
                response = new
                {
                    Result = true,
                    Message = ""
                };
                return StatusCode(StatusCodes.Status200OK, response);
            });
        }

        [HttpPost]
        [Route("TransportStart")]
        public async Task<IActionResult> TransportStart()
        {
            object response = null;
            return await Task.Run(() =>
            {
                _agv.HasJob = true;
                response = new
                {
                    Result = true,
                    Message = ""
                };
                return StatusCode(StatusCodes.Status200OK, response);
            });
        }

        //進站結束
        [HttpPost]
        [Route("TransportComplete")]
        public async Task<IActionResult> TransportComplete()
        {
            object response = null;
            return await Task.Run(() =>
            {
                response = new
                {
                    Result = true,
                    Message = ""
                };
                return StatusCode(StatusCodes.Status200OK, response);
            });
        }

        //ASRS
        [HttpPost]
        [Route("CarrierMoveoutRequest")]
        public async Task<IActionResult> CarrierMoveoutRequest()
        {
            object response = null;
            return await Task.Run(() =>
            {
                response = new
                {
                    Result = true,
                    Message = ""
                };
                return StatusCode(StatusCodes.Status200OK, response);
            });
        }

        [HttpPost]
        [Route("CarrierMoveoutComplete")]
        public async Task<IActionResult> CarrierMoveoutComplete()
        {
            object response = null;
            return await Task.Run(() =>
            {
                response = new
                {
                    Result = true,
                    Message = ""
                };
                return StatusCode(StatusCodes.Status200OK, response);
            });
        }

        [HttpPost]
        [Route("CarrierMoveinRequest")]
        public async Task<IActionResult> CarrierMoveinRequest()
        {
            object response = null;
            return await Task.Run(() =>
            {
                response = new
                {
                    Result = true,
                    Message = "",
                    StockType = "FINISH",
                    Carrier = _carrier
                };
                return StatusCode(StatusCodes.Status200OK, response);
            });
        }

        [HttpPost]
        [Route("CarrierMoveinComplete")]
        public async Task<IActionResult> CarrierMoveinComplete()
        {
            object response = null;
            return await Task.Run(() =>
            {
                response = new
                {
                    Result = true,
                    Message = ""
                };
                return StatusCode(StatusCodes.Status200OK, response);
            });
        }

    }
}
public class EAP
{
    public string Equipment { get; set; }
    public string Name { get; set; }
    public string IPAddress { get; set; }
    public int Port { get; set; }
    public string SendData { get; set; }
    public bool HasJob { get; set; }
    public bool UnloadRequest { get; set; }


    private TCP.Client _client;
    private static string _carrier = "100001";
    public EAP(string equipment, string name, string ipAddress, int port)
    {
        this.Equipment = equipment;
        this.Name = name;
        this.IPAddress = ipAddress;
        this.Port = port;
        _client = new TCP.Client(this.IPAddress, this.Port);
        _client.Connect();
        _client.ReceiveDataAsync();
        _client.OnTCPConnection += _client_OnTCPConnection;
        this.HasJob = false;
    }
    public void Close()
    {
        try
        {
            _client.OnTCPConnection -= _client_OnTCPConnection;
        }
        catch (Exception ex) { }
    }

    private void _client_OnTCPConnection(object sender, TCP.OnTCPConnectionArgs e)
    {
        //throw new NotImplementedException();
        string ret = e.Msg;
        if (ret == "") { return; }
        dynamic cmd = JsonSerializer.Deserialize<dynamic>(e.Msg);
        using (StreamWriter wr = new StreamWriter(System.AppDomain.CurrentDomain.SetupInformation.ApplicationBase + @"\EapLog.txt", true))
        {
            wr.WriteLine("Receive:" + e.TcpClient.Client.RemoteEndPoint.ToString());
            wr.WriteLine("Time:" + DateTime.Now.ToString());
            wr.WriteLine("Data:" + ret);
            wr.WriteLine("-------------------------------------------------------");
            wr.Flush();
            wr.Close();
        }
        if (bool.Parse(cmd["Result"].ToString()))
        {
            return;
        }
        //ASRS
        if (this.Equipment.Contains("STOCK01"))
        {
            if (!bool.Parse(cmd["Result"].ToString()) && !this.HasJob)
            {
                Thread.Sleep(10000);
                _client.SendData(EAP.CarrierMoveoutRequest());
                using (StreamWriter wr = new StreamWriter(System.AppDomain.CurrentDomain.SetupInformation.ApplicationBase + @"\EapLog.txt", true))
                {
                    wr.WriteLine("ASRS Send:");
                    wr.WriteLine("Time:" + DateTime.Now.ToString());
                    wr.WriteLine("Data:" + EAP.CarrierMoveoutRequest());
                    wr.WriteLine("-------------------------------------------------------");
                    wr.Flush();
                    wr.Close();
                }
            }
        }
        //AGV
        else
        {
            if (this.UnloadRequest)
            {
                //UnloadRequest
                if (!bool.Parse(cmd["Result"].ToString()))
                {
                    Thread.Sleep(10000);
                    _client.SendData(TransportRequestML02());
                    using (StreamWriter wr = new StreamWriter(System.AppDomain.CurrentDomain.SetupInformation.ApplicationBase + @"\EapLog.txt", true))
                    {
                        wr.WriteLine("AGV Send:");
                        wr.WriteLine("Time:" + DateTime.Now.ToString());
                        wr.WriteLine("Data:" + TransportRequestML02());
                        wr.WriteLine("-------------------------------------------------------");
                        wr.Flush();
                        wr.Close();
                    }
                }
            }
            else
            {
                //JobRequest
                if (!bool.Parse(cmd["Result"].ToString()) && !this.HasJob)
                {
                    Thread.Sleep(10000);
                    _client.SendData(TransportRequestStock());
                    using (StreamWriter wr = new StreamWriter(System.AppDomain.CurrentDomain.SetupInformation.ApplicationBase + @"\EapLog.txt", true))
                    {
                        wr.WriteLine("AGV Send:");
                        wr.WriteLine("Time:" + DateTime.Now.ToString());
                        wr.WriteLine("Data:" + TransportRequestStock());
                        wr.WriteLine("-------------------------------------------------------");
                        wr.Flush();
                        wr.Close();
                    }
                }
            }
        }
    }

    public void Send(string data)
    {
        this.SendData = data;
        _client.SendData(data);
    }
    public static string TransportRequestStock()
    {
        object response = new
        {
            FunctionID = "TransportRequest",
            FunctionSequenceNo = 999,
            Content = new
            {
                JobID = "A2019062410015770260003",
                TransportType = "N",
                Carrier = _carrier,
                FromEquipment = "STOCK01",
                FromPort = "2201",
                ToEquipment = "ML02",
                ToPort = "3401",
                Priority = "1"
            }
        };
        string ret = JsonSerializer.Serialize(response);
        return ret;
    }
    public static string TransportRequestML02()
    {
        object response = new
        {
            FunctionID = "TransportRequest",
            FunctionSequenceNo = 999,
            Content = new
            {
                JobID = "12345",
                TransportType = "N",
                Carrier = _carrier,
                FromEquipment = "ML02",
                FromPort = "3405",
                ToEquipment = "STOCK01",
                ToPort = "2101",
                Priority = "1"
            }
        };
        string ret = JsonSerializer.Serialize(response);
        return ret;
    }
    public static string CarrierMoveoutRequest()
    {
        object response = new
        {
            FunctionID = "CarrierMoveoutRequest",
            FunctionSequenceNo = "43",
            Content = new
            {
                JobID = "A2019062410015770260002",
                Carrier = _carrier,
                Port = "2201"
            }
        };
        string ret = JsonSerializer.Serialize(response);
        return ret;
    }
}
public class Base64
{
    public static string Decrypt(string text)
    {
        string ret = "";
        byte[] bytes = null;
        byte cut = 0x00;
        byte[] byteSpace = new byte[] { 0x14 };
        int value = 0;
        try
        {
            bytes = Encoding.GetEncoding("utf-8").GetBytes(text);
            bytes = byteCut(bytes, cut);
            text = Encoding.GetEncoding("utf-8").GetString(bytes).Trim();
            Regex regex = new Regex("[^A-Za-z0-9+/={0,3}]", RegexOptions.IgnoreCase);
            text = regex.Replace(text, "");

            text = text.Replace(Encoding.UTF8.GetString(byteSpace), "");
            //while (int.TryParse(text.Substring(text.Length - 1, 1),out value) == true) {
            //    text = text.Substring(0, text.Length - 1);
            //}
            bytes = Convert.FromBase64String(text);
            ret = Encoding.GetEncoding("utf-8").GetString(bytes);
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.ToString());
            int errCount = 10;
            try
            {
                while (errCount != 0)
                {
                    text = text.Substring(0, text.Length - 1);
                    errCount -= 1;
                    try
                    {
                        bytes = Convert.FromBase64String(text);
                        ret = Encoding.GetEncoding("utf-8").GetString(bytes);
                        goto success;
                    }
                    catch (Exception ex2)
                    {
                        Console.WriteLine(ex2.ToString());
                    }
                }
            }
            catch (Exception ex1)
            {
                Console.WriteLine(ex1.ToString());
                ret = "";
            }
        }
    success:
        return ret;
    }
    public static string Encrypt(string text)
    {
        string ret = "";
        byte[] bytes = Encoding.GetEncoding("utf-8").GetBytes(text);
        ret = Convert.ToBase64String(bytes);
        return ret;
    }
    private static byte[] byteCut(byte[] b, byte cut)
    {
        List<byte> list = new List<byte>();
        list.AddRange(b);
        for (int i = list.Count - 1; i >= 0; i--)
        {
            if (list[i] == cut)
                list.RemoveAt(i);
        }
        byte[] lastbyte = new byte[list.Count];
        for (int i = 0; i < list.Count; i++)
        {
            lastbyte[i] = list[i];
        }
        return lastbyte;
    }
}
