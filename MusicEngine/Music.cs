using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// ���y�̏����擾�o���܂��B
/// ���y�̃L���[��Component�Ɋ܂񂾃I�u�W�F�N�g�ɃA�^�b�`���Ă��������B
/// �������ɍĐ��J�n���A���̌㉹�y��̂ǂ̃^�C�~���O���A�ǂ̃u���b�N�ɂ��邩�A�Ȃǂ��擾�\�ł��B
/// �N�I���^�C�Y���čĐ��A�u���b�N�̐ݒ�A�Ȃǂ��\�ł��B
/// Known issues...
/// �����̋Ȃ̐؂�ւ��͂܂������ƃT�|�[�g���Ă܂���B
/// ���삪�d���Ȃ������͔�����ԉ\��������܂��B
/// �u���b�N�̃��[�v���ɖ���GetNumPlayedSamples��1,2�t���[���قǎ��s���邱�Ƃ��킩���Ă��܂��B
/// IMusicListener�͕K�v�ɉ����Ċg���\��ł��B
/// </summary>
public class Music : MonoBehaviour {

	/// <summary>
	/// Get a currently playing music.
	/// Be suer to play only one Music Cue at once.
	/// </summary>
	private static Music Current;
	private static List<IMusicListener> Listeners = new List<IMusicListener>();
	public interface IMusicListener
	{
		void OnMusicStarted();
		void OnBlockChanged();
	}

	//static properties
	public static int mtBar { get { return Current.mtBar_; } }
	public static int mtBeat { get { return Current.mtBeat_; } }
	public static double mtUnit { get { return Current.MusicTimeUnit; } }
	public static Timing Now { get { return Current.Now_; } }
	public static Timing Just { get { return Current.Just_; } }
	public static bool isJustChanged { get { return Current.isJustChanged_; } }
	public static bool isNowChanged { get { return Current.isNowChanged_; } }
	public static bool IsPlaying() { return Current.MusicSource.isPlaying; }
	public static void Pause() { Current.MusicSource.Pause(); }
	public static void Resume() { Current.MusicSource.Play(); }
	public static void Stop() { Current.MusicSource.Stop(); }

	/// <summary>
	/// ��ԋ߂�Just���玞�Ԃ��ǂꂾ������Ă��邩�𕄍��t���ŕԂ��B
	/// </summary>
	public static double lag
	{
		get
		{
			if ( Current.isFormerHalf_ )
				return Current.dtFromJust_;
			else
				return Current.dtFromJust_ - Current.MusicTimeUnit;
		}
	}
	/// <summary>
	/// ��ԋ߂�dmtUnit����ǂꂾ�����O�����邩���Βl�ŕԂ��B
	/// </summary>
	public static double lagAbs
	{
		get
		{
			if ( Current.isFormerHalf_ )
				return Current.dtFromJust_;
			else
				return Current.MusicTimeUnit - Current.dtFromJust_;
		}
	}
	/// <summary>
	/// lag��-1�`0�`1�̊ԂŕԂ��B
	/// </summary>
	public static double lagUnit { get { return lag / Current.MusicTimeUnit; } }

	//static predicates
	public static bool IsNowChangedWhen( System.Predicate<Timing> pred )
	{
		return Current.isNowChanged_ && pred( Current.Now_ );
	}
	public static bool IsNowChangedAt( int bar, int beat = 0, int unit = 0 )
	{
		return Current.isNowChanged_ &&
                Current.Now_.totalUnit == Current.mtBar_ * bar + Current.mtBeat_ * beat + unit;
	}
	public static bool IsJustChangedWhen( System.Predicate<Timing> pred )
	{
		return Current.isJustChanged_ && pred( Current.Just_ );
	}
	public static bool IsJustChangedAt( int bar = 0, int beat = 0, int unit = 0 )
	{
		return Current.isJustChanged_ &&
                Current.Just_.totalUnit == Current.mtBar_ * bar + Current.mtBeat_ * beat + unit;
	}

	//static funcs
	public static void QuantizePlay( AudioSource source ) { Current.QuantizedCue.Add( source ); }
	public static void AddListener( IMusicListener listener ) { Listeners.Add( listener ); }
	/*
	public static void SetNextBlock( string blockName )
	{
		int index = Current.BlockInfos.FindIndex( ( BlockInfo info ) => info.BlockName==blockName );
		if ( index >= 0 )
		{
			Current.NextBlockIndex = index;
			Current.playback.SetNextBlockIndex( index );
		}
		else
		{
			Debug.LogError( "Error!! Music.SetNextBlock Can't find block name: " + blockName );
		}
	}
	public static void SetNextBlock( int index )
	{
		if ( index < Current.CueInfo.numBlocks )
		{
			Current.NextBlockIndex = index;
			Current.playback.SetNextBlockIndex( index );
		}
		else
		{
			Debug.LogError( "Error!! Music.SetNextBlock index is out of range: " + index );
		}
	}
	public static int GetNextBlock() { return Current.NextBlockIndex; }
	public static string GetNextBlockName() { return Current.BlockInfos[Current.NextBlockIndex].BlockName; }
	public static int GetCurrentBlock() { return Current.CurrentBlockIndex; }
	public static string GetCurrentBlockName() { return Current.BlockInfos[Current.CurrentBlockIndex].BlockName; }

	public static void SetFirstBlock( int index )
	{
		if ( index < Current.CueInfo.numBlocks )
		{
			Current.NextBlockIndex = index;
			Current.CurrentBlockIndex = index;
			Current.AtomSource.Player.SetFirstBlockIndex( index );
		}
		else
		{
			Debug.LogError( "Error!! Music.SetFirstBlock index is out of range: " + index );
		}
	}
	public static void SetFirstBlock( string blockName )
	{
		int index = Current.BlockInfos.FindIndex( ( BlockInfo info ) => info.BlockName==blockName );
		if ( index >= 0 )
		{
			Current.NextBlockIndex = index;
			Current.CurrentBlockIndex = index;
			Current.AtomSource.Player.SetFirstBlockIndex( index );
		}
		else
		{
			Debug.LogError( "Error!! Music.SetFirstBlock Can't find block name: " + blockName );
		}
	}
	*/

	//static readonlies
	private static readonly int SamplingRate = 44100;

	//music editor params
	/// <summary>
	/// �ꔏ��MusicTime�����ɋ�؂��Ă��邩�B4or3���Ǝv���B
	/// </summary>
	public int mtBeat_ = 4;
	/// <summary>
	/// �ꏬ�߂�MusicTime�������B
	/// </summary>
	public int mtBar_ = 16;
	/// <summary>
	/// �ʏ�̈Ӗ��ł̉��y�̃e���|�B
	/// ���m�ɂ́AmtBeat����MusicTime��������܂ł̎��Ԃ�1���ɂ������邩�B
	/// </summary>
	public double Tempo_ = 128;

	public List<BlockInfo> BlockInfos;

	#region private params
	//music current params
	/// <summary>
	/// ��ԋ߂��^�C�~���O�ɍ��킹�Đ؂�ւ��B
	/// </summary>
	Timing Now_;
	/// <summary>
	/// �W���X�g�ɂȂ��Ă���؂�ւ��B
	/// </summary>
	Timing Just_;
	/// <summary>
	/// ���̃t���[����Timing���ω�������
	/// </summary>
	bool isJustChanged_;
	/// <summary>
	/// ���̃t���[����isFormerHalf���ω�������
	/// </summary>
	bool isNowChanged_;
	/// <summary>
	/// ���Ɣ��̊Ԃ̑O���������i�㔼�������j
	/// </summary>
	bool isFormerHalf_;
	/// <summary>
	/// mtUnit�ŏ��%����B�Ō�ɉ��y��̃^�C�~���O�����Ă���̎����ԁB
	/// </summary>
	double dtFromJust_;
	/// <summary>
	/// ADX��ł̌��ݍĐ����̃u���b�N
	/// </summary>
	int CurrentBlockIndex;
	int NumBlockBar { get { return BlockInfos[CurrentBlockIndex].NumBar; } }
	/// <summary>
	/// ADX��ł̎��ɍĐ�����\��̃u���b�N
	/// (ADX��ŏ���ɑJ�ڂ���ꍇ�͎擾�ł��Ȃ�)
	/// </summary>
	int NextBlockIndex;
	/// <summary>
	/// ���݂̃u���b�N�����s�[�g������
	/// </summary>
	int numRepeat;

	long numSamples;

	//ADX2LE objects
	AudioSource MusicSource;

	List<AudioSource> QuantizedCue;

	//readonly params
	double MusicTimeUnit;
	long SamplesPerUnit;
	long SamplesPerBeat;
	long SamplesPerBar;
	long SamplesInBlock { get { return BlockInfos[CurrentBlockIndex].NumBar * SamplesPerBar; } }
	long SamplesInMusic;

	//others
	/// <summary>
	/// Now�̒��O�̏��
	/// </summary>
	Timing Old, OldJust;
	int OldBlockIndex;
	#endregion

	#region Unity Interfaces
	void Awake()
	{
		Current = this;
		MusicSource = GetComponent<AudioSource>();
		QuantizedCue = new List<AudioSource>();

		SamplesPerUnit = (long)( SamplingRate * ( 60.0 / ( Tempo_ * mtBeat_ ) ) );
		SamplesPerBeat = SamplesPerUnit*mtBeat_;
		SamplesPerBar = SamplesPerUnit*mtBar_;

		MusicTimeUnit = (double)SamplesPerUnit / (double)SamplingRate;

		Now_ = new Timing( 0, 0, -1 );
		Just_ = new Timing( Now_ );
		Old = new Timing( Now_ );
		OldJust = new Timing( Just_ );
	}

	// Use this for initialization
	void Start()
	{
		WillBlockChange();
		MusicSource.Play();
		foreach ( IMusicListener listener in Listeners )
		{
			listener.OnMusicStarted();
		}
		OnBlockChanged();
	}

	// Update is called once per frame
	void Update()
	{
		//CurrentBlockIndex = playback.GetCurrentBlockIndex();

		numSamples = MusicSource.timeSamples;

		Just.bar = (int)( numSamples / SamplesPerBar ) % NumBlockBar;
		Just.beat = (int)( ( numSamples % SamplesPerBar ) / SamplesPerBeat );
		Just.unit = (int)( ( numSamples % SamplesPerBeat ) / SamplesPerUnit );
		isFormerHalf_ = ( numSamples % SamplesPerUnit ) < SamplesPerUnit / 2;
		dtFromJust_ = (double)( numSamples % SamplesPerUnit ) / (double)SamplingRate;

		Now.Copy( Just );
		if ( !isFormerHalf_ ) Now.Increment();
		if ( numSamples + SamplesPerUnit/2 >= SamplesInBlock )
		{
			Now.Init();
		}

		isNowChanged_ = Now.totalUnit != Old.totalUnit;
		isJustChanged_ = Just.totalUnit != OldJust.totalUnit;

		CallEvents();

		Old.Copy( Now );
		OldJust.Copy( Just );
	}

	void CallEvents()
	{
		if ( isNowChanged_ ) OnNowChanged();
		if ( isNowChanged_ && Old > Now_ )
		{
			if ( NextBlockIndex == CurrentBlockIndex )
			{
				WillBlockRepeat();
			}
			else
			{
				WillBlockChange();
			}
		}
		if ( isJustChanged_ ) OnJustChanged();
		if ( isJustChanged_ && Just_.unit == 0 ) OnBeat();
		if ( isJustChanged_ && Just_.barUnit == 0 ) OnBar();
		if ( isJustChanged_ && OldJust > Just_ )
		{
			if ( OldBlockIndex == CurrentBlockIndex )
			{
				OnBlockRepeated();
			}
			else
			{
				OnBlockChanged();
			}
			OldBlockIndex = CurrentBlockIndex;
		}
	}
	#endregion

	//On events (when isJustChanged)
	void OnNowChanged()
	{
		//foreach ( CriAtomSource cue in QuantizedCue )
		//{
		//    cue.SetAisac( 2, (float)(MusicTimeUnit - dtFromJust_) );
		//    cue.Play();
		//}
		//QuantizedCue.Clear();
	}

	void OnJustChanged()
	{
		foreach ( AudioSource cue in QuantizedCue )
		{
			//cue.SetAisac( 2, 0 );
			cue.Play();
		}
		QuantizedCue.Clear();
		//Debug.Log( "OnJust " + Just.ToString() );
	}

	void OnBeat()
	{
		//Debug.Log( "OnBeat " + Just.ToString() );
	}

	void OnBar()
	{
		//Debug.Log( "OnBar " + Just.ToString() );
	}

	void WillBlockRepeat()
	{
	}

	void WillBlockChange()
	{
	}

	void OnBlockRepeated()
	{
		++numRepeat;
		//Debug.Log( "NumRepeat = " + numRepeat );
	}

	void OnBlockChanged()
	{
		numRepeat = 0;
		foreach ( IMusicListener listener in Listeners )
		{
			listener.OnBlockChanged();
		}
	}


	[System.Serializable]
	public class BlockInfo
	{
		public BlockInfo( string BlockName, int NumBar = 4 )
		{
			this.BlockName = BlockName;
			this.NumBar = NumBar;
		}
		public string BlockName;
		public int NumBar = 4;
	}
}
