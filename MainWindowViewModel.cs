using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using TwinCAT.Ads.TypeSystem;
using TwinCAT.TypeSystem;
using TwinCAT.Ads;
using System.Configuration;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using System.Windows;
using TwinCAT;
using System.Collections;

namespace SimpleAdsClient
{
	internal class MainWindowViewModel : INotifyPropertyChanged
	{
		#region Properties

		public bool Connected
		{
			get => connected;
			set
			{
				if (connected != value)
				{
					connected = value;
					raisePropertyChanged();
					raisePropertyChanged(nameof(Disconnected));
				}
			}
		}

		public bool Disconnected
		{
			get => !connected;
			set	{ Connected = !value; }
		}

		public string AmsNetIdText
		{
			get => amsNetId;
			set
			{
				if (amsNetId != value)
				{
					amsNetId = value;
					raisePropertyChanged();
				}
			}
		}

		public ushort AmsPort
		{
			get => amsPort;
			set
			{
				if (amsPort != value)
				{
					amsPort = value;
					raisePropertyChanged();
				}
			}
		}

		public object Symbols
		{
			get => symbols;
			set
			{
				if (!Equals(Symbols, value))
				{
					symbols = value;
					raisePropertyChanged();
				}
			}
		}

		public string Path
		{
			get => path;
			set
			{
				if (path != value)
				{
					path = value;
					raisePropertyChanged();
				}
			}
		}

		public string DataType
		{
			get => dataType;
			set
			{
				if (dataType != value)
				{
					dataType = value;
					raisePropertyChanged();
				}
			}
		}

		public string IndexGroup
		{
			get => indexGroup;
			set
			{
				if (indexGroup != value)
				{
					indexGroup = value;
					raisePropertyChanged();
				}
			}
		}

		public string IndexOffset
		{
			get => indexOffset;
			set
			{
				if (indexOffset != value)
				{
					indexOffset = value;
					raisePropertyChanged();
				}
			}
		}

		public string ClrType
		{
			get => clrType;
			set
			{
				if (clrType != value)
				{
					clrType = value;
					raisePropertyChanged();
				}
			}
		}

		public string ArrayDimensions
		{
			get => arrayDimensions;
			set
			{
				if (arrayDimensions != value)
				{
					arrayDimensions = value;
					raisePropertyChanged();
				}
			}
		}

		public string ArrayElementSize
		{
			get => arrayElementSize;
			set
			{
				if (arrayElementSize != value)
				{
					arrayElementSize = value;
					raisePropertyChanged();
				}
			}
		}

		public string Methods
		{
			get => methods;
			set
			{
				if (methods != value)
				{
					methods = value;
					raisePropertyChanged();
				}
			}
		}

		public string Value
		{
			get => val;
			set
			{
				if (val != value)
				{
					val = value;
					raisePropertyChanged();
				}
			}
		}

		public ICommand ConnectCommand { get; }
		public ICommand DisconnectCommand { get; }

		#endregion

		#region Events

		public event PropertyChangedEventHandler PropertyChanged;

		#endregion

		#region Constructors

		public MainWindowViewModel() 
		{
			ConnectCommand = new RelayCommand(connect);
			DisconnectCommand = new RelayCommand(disconnect);

			AmsNetIdText = ConfigurationManager.AppSettings["defaultAmsNetId"] ?? "127.0.0.1.1.1";
			if (!ushort.TryParse(ConfigurationManager.AppSettings["defaultAmsPort"], out amsPort))
				AmsPort = 851;
		}

		#endregion

		#region Methods 

		public void UpdateSelection(DynamicSymbol dynamicSymbol)
		{
			if (dynamicSymbol == null)
			{
				Path = "";
				DataType = "";
				IndexGroup = "";
				IndexOffset = "";
				ClrType = "";
				ArrayDimensions = "";
				ArrayElementSize = "";
				Methods = "";
				Value = "";

				return;
			}

			IAdsSymbol adsSymbol = getAdsSymbolBySymbol(dynamicSymbol);

			Path = adsSymbol?.InstancePath;
			DataType = adsSymbol?.DataType?.Name;
			IndexGroup = (adsSymbol != null) ? adsSymbol.IndexGroup.ToString() : "";
			IndexOffset = (adsSymbol != null) ? adsSymbol.IndexOffset.ToString() : "";
			ClrType = getClrType(adsSymbol)?.FullName ?? "< not supported >";

			IDimensionCollection dimensions = getArrayDimensions(adsSymbol);

			ArrayDimensions = dimensionsToString(dimensions);
			ArrayElementSize = getElementSize(adsSymbol).ToString();

			if (adsSymbol != null && adsSymbol.DataType is IStructType rpcStruct)
			{
				StringBuilder methodsString = new StringBuilder();

				foreach (IRpcMethod method in rpcStruct.RpcMethods.Where(m => !m.Name.StartsWith("__")))
				{
					methodsString.Append(method.IsVoid ? "void " : method.ReturnType + " ");

					methodsString.Append(method.Name + "(");

					foreach (var inParameter in method.InParameters)
						methodsString.Append("in " + inParameter.TypeName + " " + inParameter.Name + ",");

					foreach (var outParameter in method.OutParameters)
						methodsString.Append("out " + outParameter.TypeName + " " + outParameter.Name + ",");

					if (methodsString[methodsString.Length - 1] == ',')
						methodsString.Remove(methodsString.Length - 1, 1);

					methodsString.Append(")\n");
				}

				Methods = methodsString.ToString();
			}
			else
				Methods = "";

			try
			{
				dynamic value = dynamicSymbol.ReadValue();

				DynamicValue dynamicValue = value as DynamicValue;

				if (!(value is string) && value is IEnumerable)
				{
					StringBuilder arrayString = new StringBuilder();

					Array result = adsClient.ReadValue(adsSymbol) as Array;

					ArraySegment<object> arraySegment = new ArraySegment<object>(result.Cast<object>().ToArray(), 0, dimensions[0].ElementCount);

					Array subArray = arraySegment.ToArray();

					foreach (object item in subArray)
					{
						if (arrayString.Length > 0)
							arrayString.Append(",");

						if (item != null)
							arrayString.Append(item.ToString());
					}

					arrayString.Insert(0, "[");
					arrayString.Append("]");

					Value = arrayString.ToString();
				}
				else if (Equals(null, value))
					Value = "< null >";
				else
					Value = value.ToString();
			}
			catch (Exception ex)
			{
				Value = "< " + ex.Message + " >";
			}
		}

		#endregion

		#region Not public memebers

		#region Fields

		private readonly TwinCAT.Ads.AdsClient adsClient = new TwinCAT.Ads.AdsClient();
		private readonly Dictionary<DynamicSymbol, IAdsSymbol> symbolInfos = new Dictionary<DynamicSymbol, IAdsSymbol>();

		private bool connected;

		private string amsNetId;
		private ushort amsPort;
		private object symbols;
		private string path;
		private string dataType;
		private string indexGroup;
		private string indexOffset;
		private string clrType;
		private string arrayDimensions;
		private string arrayElementSize;
		private string methods;
		private string val;

		#endregion

		#region Methods

		private void raisePropertyChanged([CallerMemberName] string propertyName = "")
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}

		private void connect(object parameter)
		{
			symbolInfos.Clear();

			if (!AmsNetId.TryParse(AmsNetIdText, out AmsNetId amsNetId))
			{
				MessageBox.Show("Invalid AMS net ID '" + AmsNetIdText + "'");
				return;
			}

			try
			{
				adsClient.Connect(amsNetId, amsPort);

				IDynamicSymbolLoader symbolLoader = (IDynamicSymbolLoader)SymbolLoaderFactory.Create(adsClient, SymbolLoaderSettings.DefaultDynamic);

				Symbols = symbolLoader.SymbolsDynamic;

				Connected = true;
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.GetBaseException().Message);
			}
		}

		private bool canConnect(object arg)
		{
			return !connected;
		}

		private void disconnect(object parameter)
		{
			try
			{
				Symbols = null;

				adsClient.Disconnect();
			}
			catch { }

			Connected = false;
		}

		private bool canDisconnect(object arg)
		{
			return connected;
		}

		private IAdsSymbol getAdsSymbolBySymbol(DynamicSymbol symbol)
		{
			if (symbol == null)
				return null;

			if (!symbolInfos.TryGetValue(symbol, out IAdsSymbol symbolInfo))
			{
				try
				{
					symbolInfo = adsClient.ReadSymbol(symbol.InstancePath);
				}
				catch { }

				symbolInfos.Add(symbol, symbolInfo);
			}

			return symbolInfo;
		}

		private Type getClrType(IAdsSymbol adsSymbol)
		{
			if (adsSymbol == null || adsSymbol.DataType == null)
				return null;

			Type result = adsSymbol.DataType.GetType().GetProperty("ManagedType")?.GetValue(adsSymbol.DataType) as Type;

			if (result != null)
				return result;

			return null;
		}

		private IDimensionCollection getArrayDimensions(IAdsSymbol adsSymbol)
		{
			if (adsSymbol == null || !(adsSymbol.DataType is ArrayType arrayType))
				return null;

			if (arrayType.Dimensions == null || arrayType.Dimensions.Count == 0)
				return null;

			return arrayType.Dimensions;
		}

		private string dimensionsToString(IDimensionCollection dimensions)
		{
			if (dimensions == null || dimensions.Count == 0)
				return "";

			StringBuilder result = new StringBuilder();

			foreach (Dimension dimension in dimensions)
			{
				if (result.Length > 0)
					result.Append(",");

				result.Append("[" + dimension.LowerBound + ".." + dimension.UpperBound + "]");
			}

			return result.ToString();
		}

		private int? getElementSize(IAdsSymbol adsSymbol)
		{
			if (adsSymbol == null || !(adsSymbol.DataType is ArrayType arrayType))
				return null;

			return arrayType.ElementSize;
		}

		#endregion

		#endregion
	}
}
