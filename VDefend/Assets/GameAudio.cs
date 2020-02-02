using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameAudio : AudioManagerBase {
    Dictionary<string, AudioClip> m_BGMClips=new Dictionary<string, AudioClip>();
    Dictionary<string, AudioClip> m_AudioClips=new Dictionary<string, AudioClip>();
    public override void Init()
    {
        base.Init();
        TResources.LoadAll<AudioClip>("BGM").Traversal((AudioClip clip)=> {
            m_BGMClips.Add(clip.name, clip);
        });
        TResources.LoadAll<AudioClip>("Audio").Traversal((AudioClip clip) => {
            m_AudioClips.Add(clip.name, clip);
        });
    }

    public void Play(string clip,int extraCount=-1)
    {
        if (extraCount > 0)
            clip += "_" + Random.Range(0, extraCount);
        PlayClip(-1,m_AudioClips[clip],1f,false);
    }
    public void SwitchBGM(bool inGame)=>  SwitchBackground(m_BGMClips[inGame?"game":"menu"],true);
}
