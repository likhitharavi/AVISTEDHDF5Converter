using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using HDF5DotNet;
using Newtonsoft.Json;
using System.IO;
using System.Text;
using System.Configuration;
using NLog;

namespace MvcApplication1.Controllers
{
    public class ValuesController : ApiController
    {
        // GET api/values
        private static Logger logger = LogManager.GetCurrentClassLogger();
        public string Get(string path,bool outfolder)
        {
            logger.Log(LogLevel.Info, "Entered AVISTEDHDF5Converter GET()");
            try
            {
                string content = File.ReadAllText(path);
                List<Dictionary<string, string>> data = JsonConvert.DeserializeObject<List<Dictionary<string, string>>>(content);

                string result = "false";

                string randomlyGeneratedFolderNamePart = Path.GetFileNameWithoutExtension(Path.GetRandomFileName());

                string timeRelatedFolderNamePart = DateTime.Now.Year.ToString()
                                                 + DateTime.Now.Month.ToString()
                                                 + DateTime.Now.Day.ToString()
                                                 + DateTime.Now.Hour.ToString()
                                                 + DateTime.Now.Minute.ToString()
                                                 + DateTime.Now.Second.ToString()
                                                 + DateTime.Now.Millisecond.ToString();

                string processRelatedFolderNamePart = System.Diagnostics.Process.GetCurrentProcess().Id.ToString();
                string copypath = "";
                if (outfolder)
                {
                    copypath = ConfigurationManager.AppSettings["Save_Downloads"].ToString();
                }
                else
                {
                    copypath = ConfigurationManager.AppSettings["Converters"].ToString();
                }
                string temporaryDirectoryName = Path.Combine(copypath
                                                            , timeRelatedFolderNamePart
                                                            + processRelatedFolderNamePart
                                                            + randomlyGeneratedFolderNamePart);
                Directory.CreateDirectory(temporaryDirectoryName);
                
                logger.Log(LogLevel.Info, "Created Directory");
                string uri = Path.Combine(temporaryDirectoryName, "result" + ".h5");
                H5FileId fileId = H5F.create(uri,
                        H5F.CreateMode.ACC_TRUNC);
                string[] results = new string[data.Count + 1];
                int i = 0, j = 0;
                Dictionary<string, string> resultdict = new Dictionary<string, string>();
                Dictionary<string, string> tempdict = data.First();

                string[] names = tempdict.Keys.ToArray();
                string[] values = new string[names.Length];
                foreach (Dictionary<string, string> dict in data)
                {
                    var value = dict.Values.ToArray();

                    if (j == 0)
                    {
                        for (int k = 0; k < values.Length; k++)
                        {
                            values[k] = value[k];
                        }
                        j = 1;

                    }
                    else
                    {
                        for (int k = 0; k < values.Length; k++)
                        {
                            values[k] += "," + value[k];
                        }

                    }
                }
                int index = 0;
                foreach (string s in names)
                {
                    if (s.Equals("date"))
                    {
                        string[] strings = values[index++].Split(',');
                        byte[] bytes = Encoding.UTF8.GetBytes(String.Concat(strings));     
                        char[,] myChars = new char[strings.Length, 10];
                        myChars = StringToChar(myChars, strings);
                        
                        // Prepare to 9create a data space for writing a 1 - dimensional
                        // signed integer array.
                        long[] dims = new long[1];
                        dims[0] = strings.Length;
                        H5DataSpaceId spaceId = H5S.create_simple(1, dims);
                        H5DataTypeId typeId = H5T.copy(H5T.H5Type.C_S1);
                       
                        // Find the size of the type
                        int typeSize = H5T.getSize(typeId) * 10;
                        H5T.setSize(typeId, 10);
                        string name = "/" + s;
                       
                        // Create the data set.
                        H5DataSetId dataSetId = H5D.create(fileId, s,
                            typeId, spaceId);
                        H5D.write(dataSetId, typeId,
                            new H5Array<byte>(bytes));
                        H5D.close(dataSetId);
                        H5S.close(spaceId);
                        logger.Log(LogLevel.Info, "Created parameter {0}", s);
                     }
                    else
                    {
                        string[] strings = values[index++].Split(',');
                        float[] vl = new float[strings.Length];
                        int l = 0;
                        foreach (string d in strings)
                        {
                            vl[l++] = float.Parse(d);
                        }
                        // Prepare to create a data space for writing a 1 - dimensional
                        // signed integer array.
                        long[] dims = new long[1];
                        dims[0] = strings.Length;
                        H5DataSpaceId spaceId = H5S.create_simple(1, dims);
                        H5DataTypeId typeId1 = H5T.copy(H5T.H5Type.NATIVE_FLOAT);

                        // Find the size of the type
                        int typeSize = H5T.getSize(typeId1);
                        // Set the order to big endian
                        H5T.setOrder(typeId1, H5T.Order.BE);

                        // Set the order to little endian
                        H5T.setOrder(typeId1, H5T.Order.LE);
                        string name = "/" + s;
                        // Create the data set.
                        H5DataSetId dataSetId = H5D.create(fileId, s,
                            typeId1, spaceId);

                        H5D.write(dataSetId, new H5DataTypeId(H5T.H5Type.NATIVE_FLOAT),
                            new H5Array<float>(vl));
                        //  dscopy.AddVariable<float>(s, vl);
                        H5D.close(dataSetId);
                        H5S.close(spaceId);
                        H5T.close(typeId1);
                        logger.Log(LogLevel.Info, "Created parameter {0}", s);
                    }
                }
                 
                H5F.close(fileId);
                string SourceFolderPath = temporaryDirectoryName;
                return SourceFolderPath;
            }
            catch(Exception ex)
            {
                logger.Error("AVISTEDHDF5Converter:Failed with exception {0}", ex.Message);
            }
            return "Error";

        }
        public char[,] StringToChar(char[,] charArray, string[] rows)
        {
            for (int i = 0; i < rows.Length; i++)
                for (int j = 0; j < rows[i].Length; j++)
                {
                    charArray[i, j] = rows[i][j];
                }
            return charArray;
        }
        // GET api/values/5
        public string Get(int id)
        {
            return "value";
        }

        // POST api/values
        public void Post([FromBody]string value)
        {
        }

        // PUT api/values/5
        public void Put(int id, [FromBody]string value)
        {
        }

        // DELETE api/values/5
        public void Delete(int id)
        {
        }
    }
}