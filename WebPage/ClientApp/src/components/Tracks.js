import React, { Component } from 'react';

export class Tracks extends Component {
    static displayName = Tracks.name;

    constructor(props) {
        super(props);
        this.state = {
            tracks: [],
            loading: true,
            filePath: "",
            user: "",
            force: false
        };

        this.postTrack = this.postTrack.bind(this);
        this.changeFilePath = this.changeFilePath.bind(this);
        this.changeForced = this.changeForced.bind(this);
    }

    componentDidMount() {
        this.FetchTracks();
    }

    static renderTrackTable(tracks) {
        return (
            <table className='table table-striped' aria-labelledby="tabelLabel">
                <thead>
                    <tr>
                        <th>trackId</th>
                        <th>trackReference</th>
                        <th>trackDuration</th>
                        <th>trackDiskoteketId</th>
                        <th>trackDiskoteketSideNr</th>
                        <th>trackDiskoteketTrackNr</th>
                        <th>Updated</th>
                    </tr>
                </thead>
                <tbody>
                    {tracks.map(f =>
                        <tr key={f.id}>
                            <td>{f.id}</td>
                            <td>{f.reference}</td>
                            <td nowrap>{new Date(f.duration).toISOString().substr(11, 8)}</td>
                            <td>{f.drDiskoteksnr}</td>
                            <td>{f.sidenummer}</td>
                            <td>{f.sekvensnummer}</td>
                            <td nowrap>{f.dateChanged}</td>
                        </tr>
                    )}
                </tbody>
            </table>
        );
    }

    changeFilePath(event) {
        this.setState({ filePath: event.target.value });
    }
    changeForced(event) {
        var bol = this.state.force;
        console.log(!bol)
        this.setState({ force: !bol });
    }

    render() {
        let contents = this.state.loading
            ? <p><em>Loading...</em></p>
            : Tracks.renderTrackTable(this.state.tracks);

        return (
            <div>
                <h5 id="tabelLabel" >Tracks</h5>

                <form onSubmit={this.postTrack}>
                    <label>
                        File Path:
                <input type="text" size="100" name="File Path" onChange={this.changeFilePath} />
                    </label><br />
                    Force <input type="checkbox" name="force" checked={this.state.force} onChange={this.changeForced} />
                    <input type="submit" text="Fingerprint track" />
                </form>


                <br />
                {contents}
            </div>
        );
    }

    async postTrack(e) {
        e.preventDefault();
        const input = { "path": this.state.filePath };
        const settings = {
            method: "post",
            headers: {
                'Accept': 'application/json',
                'Content-Type': 'application/json'
            },
            body: JSON.stringify(input)
        };
        const url = "api/tracks?force=" + this.state.force;

        const response = await fetch(url, settings);
        const data = await response.text();

        this.setState({ loading: false });
        alert("A track was submitted for fingerprinting: " + data);
        this.FetchTracks();
    }

    async FetchTracks() {
        this.setState({ loading: true });
        const response = await fetch('api/tracks');
        const data = await response.json();
        this.setState({ tracks: data, loading: false });
    }
}
